using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Milvus.Client;

namespace SE26Project_18.Api.Infrastructure.VectorStore;

internal sealed class MilvusVectorStore : IVectorStore, IDisposable
{
    private const string IdFieldName = "id";

    private const int MaximumSearchLimit = 16_384;

    private static readonly TimeSpan InitializationTimeout = TimeSpan.FromMinutes(5);

    private readonly MilvusClient _client;

    private readonly ILogger<MilvusVectorStore> _logger;

    private readonly ConcurrentDictionary<string, VectorIndexDefinition> _definitions = new(
        StringComparer.Ordinal
    );

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _indexLocks = new(
        StringComparer.Ordinal
    );

    private int _disposed;

    public MilvusVectorStore(IOptions<MilvusOptions> options, ILogger<MilvusVectorStore> logger)
    {
        var configuration = options.Value;
        _logger = logger;

        if (
            string.IsNullOrWhiteSpace(configuration.UserName)
            != string.IsNullOrWhiteSpace(configuration.Password)
        )
        {
            throw new InvalidOperationException(
                "Milvus username and password must be configured together."
            );
        }

        _client = string.IsNullOrWhiteSpace(configuration.UserName)
            ? new MilvusClient(
                configuration.HostName,
                configuration.Port,
                configuration.UseTls,
                configuration.DatabaseName
            )
            : new MilvusClient(
                configuration.HostName,
                configuration.UserName,
                configuration.Password!,
                configuration.Port,
                configuration.UseTls,
                configuration.DatabaseName
            );
    }

    public async Task EnsureIndexAsync(VectorIndexDefinition definition, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(definition);
        ValidateDefinition(definition);

        if (_definitions.TryGetValue(definition.Name, out var knownDefinition))
        {
            ThrowIfDefinitionsDiffer(knownDefinition, definition);
            return;
        }

        var indexLock = _indexLocks.GetOrAdd(definition.Name, _ => new SemaphoreSlim(1, 1));
        await indexLock.WaitAsync(ct);

        try
        {
            if (_definitions.TryGetValue(definition.Name, out knownDefinition))
            {
                ThrowIfDefinitionsDiffer(knownDefinition, definition);
                return;
            }

            var collection = _client.GetCollection(definition.Name);
            if (await _client.HasCollectionAsync(definition.Name, cancellationToken: ct))
            {
                await ValidateExistingSchemaAsync(collection, definition, ct);
            }
            else
            {
                try
                {
                    await _client.CreateCollectionAsync(
                        definition.Name,
                        CreateSchema(definition),
                        cancellationToken: ct
                    );
                }
                catch (MilvusException)
                {
                    if (!await _client.HasCollectionAsync(definition.Name, cancellationToken: ct))
                        throw;

                    await ValidateExistingSchemaAsync(collection, definition, ct);
                }
            }

            foreach (var field in definition.Fields)
            {
                await EnsureVectorIndexAsync(collection, definition, field, ct);
            }

            await collection.LoadAsync(cancellationToken: ct);
            await collection.WaitForCollectionLoadAsync(
                Array.Empty<string>(),
                timeout: InitializationTimeout,
                cancellationToken: ct
            );
            _definitions[definition.Name] = definition;
            _logger.LogInformation("Milvus index {IndexName} is ready", definition.Name);
        }
        finally
        {
            indexLock.Release();
        }
    }

    public async Task UpsertAsync(VectorRecord record, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(record);
        var definition = GetEnsuredDefinition(record.IndexName);
        ValidateRecord(record, definition);

        var data = new List<FieldData> { FieldData.Create(IdFieldName, new[] { record.Id }) };
        foreach (var field in definition.Fields)
        {
            data.Add(FieldData.CreateFloatVector(field.Name, new[] { record.Vectors[field.Name] }));
        }

        await _client.GetCollection(record.IndexName).UpsertAsync(data, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken ct
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(request);
        var definition = GetEnsuredDefinition(request.IndexName);
        ValidateSearchRequest(request, definition);

        var result = await _client
            .GetCollection(request.IndexName)
            .SearchAsync(
                request.VectorFieldName,
                new[] { request.QueryVector },
                ToMilvusMetric(definition.Metric),
                request.Limit,
                cancellationToken: ct
            );
        var ids = result.Ids.LongIds;

        if (ids is null)
            throw new InvalidOperationException(
                $"The index '{request.IndexName}' did not return Int64 primary keys."
            );

        if (ids.Count != result.Scores.Count)
            throw new InvalidOperationException(
                $"The index '{request.IndexName}' returned mismatched search IDs and scores."
            );

        var matches = new List<VectorSearchResult>(ids.Count);
        for (var i = 0; i < ids.Count; i++)
        {
            matches.Add(new VectorSearchResult(ids[i], result.Scores[i]));
        }

        return matches;
    }

    public async Task DeleteAsync(
        string indexName,
        IReadOnlyCollection<long> ids,
        CancellationToken ct
    )
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(ids);
        ct.ThrowIfCancellationRequested();
        if (ids.Count == 0)
            return;

        _ = GetEnsuredDefinition(indexName);
        var expression = $"{IdFieldName} in [{string.Join(",", ids)}]";
        await _client.GetCollection(indexName).DeleteAsync(expression, cancellationToken: ct);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _client.Dispose();

        foreach (var indexLock in _indexLocks.Values)
        {
            indexLock.Dispose();
        }
    }

    private static IReadOnlyList<FieldSchema> CreateSchema(VectorIndexDefinition definition)
    {
        var fields = new List<FieldSchema>
        {
            FieldSchema.Create<long>(IdFieldName, isPrimaryKey: true),
        };
        fields.AddRange(
            definition.Fields.Select(field =>
                FieldSchema.CreateFloatVector(field.Name, field.Dimension)
            )
        );

        return fields;
    }

    private static async Task ValidateExistingSchemaAsync(
        MilvusCollection collection,
        VectorIndexDefinition definition,
        CancellationToken ct
    )
    {
        var schema = (await collection.DescribeAsync(ct)).Schema;
        var expectedFieldCount = definition.Fields.Count + 1;
        if (schema.Fields.Count != expectedFieldCount)
        {
            throw new InvalidOperationException(
                $"The existing index '{definition.Name}' does not match its expected schema."
            );
        }

        var idField = schema.Fields.SingleOrDefault(field => field.Name == IdFieldName);
        if (
            idField is null
            || !idField.IsPrimaryKey
            || idField.AutoId
            || idField.DataType != MilvusDataType.Int64
        )
        {
            throw new InvalidOperationException(
                $"The existing index '{definition.Name}' does not have the expected Int64 '{IdFieldName}' primary key."
            );
        }

        foreach (var expectedField in definition.Fields)
        {
            var actualField = schema.Fields.SingleOrDefault(field =>
                field.Name == expectedField.Name
            );
            if (
                actualField is null
                || actualField.DataType != MilvusDataType.FloatVector
                || actualField.Dimension != expectedField.Dimension
            )
            {
                throw new InvalidOperationException(
                    $"The existing index '{definition.Name}' vector field '{expectedField.Name}' does not match its expected schema."
                );
            }
        }
    }

    private VectorIndexDefinition GetEnsuredDefinition(string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            throw new ArgumentException("An index name is required.", nameof(indexName));

        return _definitions.TryGetValue(indexName, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"The index '{indexName}' must be ensured before it can be used."
            );
    }

    private static void ValidateDefinition(VectorIndexDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition.Fields);

        if (string.IsNullOrWhiteSpace(definition.Name))
            throw new ArgumentException("An index name is required.", nameof(definition));

        if (definition.Fields.Count == 0)
            throw new ArgumentException(
                "An index must contain at least one vector field.",
                nameof(definition)
            );

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in definition.Fields)
        {
            ArgumentNullException.ThrowIfNull(field);

            if (string.IsNullOrWhiteSpace(field.Name))
                throw new ArgumentException("A vector field name is required.", nameof(definition));

            if (field.Name == IdFieldName)
                throw new ArgumentException(
                    $"'{IdFieldName}' is reserved for the primary key field.",
                    nameof(definition)
                );

            if (field.Dimension <= 0)
                throw new ArgumentException(
                    "A vector field dimension must be positive.",
                    nameof(definition)
                );

            if (!names.Add(field.Name))
                throw new ArgumentException(
                    "Vector field names must be unique.",
                    nameof(definition)
                );
        }
    }

    private static void ValidateRecord(VectorRecord record, VectorIndexDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(record.Vectors);

        if (record.Vectors.Count != definition.Fields.Count)
        {
            throw new ArgumentException(
                $"The record for index '{record.IndexName}' must contain every vector field.",
                nameof(record)
            );
        }

        foreach (var field in definition.Fields)
        {
            if (!record.Vectors.TryGetValue(field.Name, out var vector))
            {
                throw new ArgumentException(
                    $"The record for index '{record.IndexName}' is missing vector field '{field.Name}'.",
                    nameof(record)
                );
            }

            if (vector.Length != field.Dimension)
            {
                throw new ArgumentException(
                    $"Vector field '{field.Name}' must have dimension {field.Dimension}.",
                    nameof(record)
                );
            }

            var squaredNorm = 0d;
            foreach (var component in vector.Span)
            {
                if (!float.IsFinite(component))
                {
                    throw new ArgumentException(
                        $"Vector field '{field.Name}' must contain only finite values.",
                        nameof(record)
                    );
                }

                squaredNorm += component * component;
            }

            if (definition.Metric == VectorDistanceMetric.Cosine && squaredNorm == 0d)
            {
                throw new ArgumentException(
                    $"Vector field '{field.Name}' cannot be a zero vector when using cosine similarity.",
                    nameof(record)
                );
            }
        }
    }

    private static void ValidateSearchRequest(
        VectorSearchRequest request,
        VectorIndexDefinition definition
    )
    {
        var field = definition.Fields.SingleOrDefault(field =>
            field.Name == request.VectorFieldName
        );
        if (field is null)
        {
            throw new ArgumentException(
                $"The index '{request.IndexName}' does not contain vector field '{request.VectorFieldName}'.",
                nameof(request)
            );
        }

        if (request.QueryVector.Length != field.Dimension)
        {
            throw new ArgumentException(
                $"Query vector field '{request.VectorFieldName}' must have dimension {field.Dimension}.",
                nameof(request)
            );
        }

        var squaredNorm = 0d;
        foreach (var component in request.QueryVector.Span)
        {
            if (!float.IsFinite(component))
            {
                throw new ArgumentException(
                    "Query vectors must contain only finite values.",
                    nameof(request)
                );
            }

            squaredNorm += component * component;
        }

        if (definition.Metric == VectorDistanceMetric.Cosine && squaredNorm == 0d)
        {
            throw new ArgumentException(
                "Query vectors cannot be zero vectors when using cosine similarity.",
                nameof(request)
            );
        }

        if (request.Limit is < 1 or > MaximumSearchLimit)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"Search limit must be between 1 and {MaximumSearchLimit}."
            );
        }
    }

    private static bool DefinitionsMatch(VectorIndexDefinition first, VectorIndexDefinition second)
    {
        return first.Name == second.Name
            && first.Metric == second.Metric
            && first.Fields.Count == second.Fields.Count
            && first
                .Fields.OrderBy(field => field.Name)
                .SequenceEqual(second.Fields.OrderBy(field => field.Name));
    }

    private static string GetIndexName(VectorFieldDefinition field)
    {
        return $"{field.Name}_flat_index";
    }

    private static void ThrowIfDefinitionsDiffer(
        VectorIndexDefinition knownDefinition,
        VectorIndexDefinition definition
    )
    {
        if (!DefinitionsMatch(knownDefinition, definition))
        {
            throw new InvalidOperationException(
                $"The index '{definition.Name}' was already ensured with a different definition."
            );
        }
    }

    private async Task EnsureVectorIndexAsync(
        MilvusCollection collection,
        VectorIndexDefinition definition,
        VectorFieldDefinition field,
        CancellationToken ct
    )
    {
        var indexName = GetIndexName(field);
        var indexes = await collection.DescribeIndexAsync(field.Name, cancellationToken: ct);
        var index = indexes.SingleOrDefault(candidate => candidate.IndexName == indexName);

        if (index is null && indexes.Count != 0)
        {
            throw new InvalidOperationException(
                $"The existing index '{definition.Name}' vector field '{field.Name}' uses an unexpected Milvus index."
            );
        }

        if (index is null)
        {
            try
            {
                await collection.CreateIndexAsync(
                    field.Name,
                    IndexType.Flat,
                    ToMilvusMetric(definition.Metric),
                    indexName,
                    cancellationToken: ct
                );
            }
            catch (MilvusException)
            {
                indexes = await collection.DescribeIndexAsync(field.Name, cancellationToken: ct);
                index = indexes.SingleOrDefault(candidate => candidate.IndexName == indexName);
                if (index is null)
                    throw;
            }

            indexes = await collection.DescribeIndexAsync(field.Name, cancellationToken: ct);
            index = indexes.SingleOrDefault(candidate => candidate.IndexName == indexName);
            if (index is null)
            {
                throw new InvalidOperationException(
                    $"Milvus did not create the expected index '{indexName}' for vector field '{field.Name}'."
                );
            }
        }

        ValidateExistingIndex(index, definition, field);
        await collection.WaitForIndexBuildAsync(
            field.Name,
            indexName,
            timeout: InitializationTimeout,
            cancellationToken: ct
        );
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
    }

    private static void ValidateExistingIndex(
        MilvusIndexInfo index,
        VectorIndexDefinition definition,
        VectorFieldDefinition field
    )
    {
        if (index.State == IndexState.Failed)
        {
            throw new InvalidOperationException(
                $"Milvus index '{index.IndexName}' for vector field '{field.Name}' failed: {index.IndexStateFailReason}"
            );
        }

        if (
            !index.Params.TryGetValue("index_type", out var indexType)
            || !string.Equals(indexType, "FLAT", StringComparison.OrdinalIgnoreCase)
            || !index.Params.TryGetValue("metric_type", out var metricType)
            || !string.Equals(
                metricType,
                ToMilvusMetric(definition.Metric).ToString(),
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new InvalidOperationException(
                $"Milvus index '{index.IndexName}' for vector field '{field.Name}' does not match its expected configuration."
            );
        }
    }

    private static SimilarityMetricType ToMilvusMetric(VectorDistanceMetric metric)
    {
        return metric switch
        {
            VectorDistanceMetric.Cosine => SimilarityMetricType.Cosine,
            VectorDistanceMetric.InnerProduct => SimilarityMetricType.Ip,
            VectorDistanceMetric.Euclidean => SimilarityMetricType.L2,
            _ => throw new ArgumentOutOfRangeException(
                nameof(metric),
                metric,
                "Unsupported vector metric."
            ),
        };
    }
}
