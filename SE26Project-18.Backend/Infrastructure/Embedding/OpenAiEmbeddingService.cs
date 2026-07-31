using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SE26Project_18.Backend.Exceptions;

namespace SE26Project_18.Backend.Infrastructure.Embedding;

internal sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;

    private readonly IMemoryCache _cache;

    private readonly EmbeddingOptions _options;

    public OpenAiEmbeddingService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<EmbeddingOptions> options
    )
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IReadOnlyDictionary<string, ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyCollection<string> texts,
        CancellationToken ct
    )
    {
        var distinctTexts = texts.Distinct(StringComparer.Ordinal).ToArray();
        var result = new Dictionary<string, ReadOnlyMemory<float>>(StringComparer.Ordinal);
        var missingTexts = new List<string>();

        foreach (var text in distinctTexts)
        {
            var key = GetCacheKey(text);
            if (_cache.TryGetValue<float[]>(key, out var cached) && cached is not null)
            {
                result[text] = cached;
            }
            else
            {
                missingTexts.Add(text);
            }
        }

        if (missingTexts.Count == 0)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new ServiceUnavailableException("Embedding API key is not configured.");
        }

        foreach (var batch in missingTexts.Chunk(_options.RequestBatchSize))
        {
            var normalizedBaseUrl = _options.BaseUrl.EndsWith('/')
                ? _options.BaseUrl
                : $"{_options.BaseUrl}/";
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(new Uri(normalizedBaseUrl), "embeddings")
            );
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey
            );
            request.Content = JsonContent.Create(
                new EmbeddingRequest(
                    _options.Model,
                    batch,
                    _options.SendDimensions ? _options.Dimension : null
                )
            );

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                if (responseBody.Length > 2_000)
                {
                    responseBody = responseBody[..2_000];
                }

                throw new ServiceUnavailableException(
                    $"Embedding API returned status {(int)response.StatusCode}: {responseBody}"
                );
            }
            var payload =
                await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct)
                ?? throw new ServiceUnavailableException(
                    "Embedding API returned an empty response."
                );
            if (payload.Data.Count != batch.Length)
            {
                throw new ServiceUnavailableException(
                    "Embedding API returned an unexpected result count."
                );
            }

            foreach (var item in payload.Data)
            {
                if (item.Index < 0 || item.Index >= batch.Length)
                {
                    throw new ServiceUnavailableException(
                        "Embedding API returned an invalid result index."
                    );
                }
                if (item.Embedding.Length != _options.Dimension)
                {
                    throw new ServiceUnavailableException(
                        $"Embedding API returned dimension {item.Embedding.Length}; expected {_options.Dimension}."
                    );
                }

                var text = batch[item.Index];
                _cache.Set(GetCacheKey(text), item.Embedding, TimeSpan.FromHours(12));
                result[text] = item.Embedding;
            }
        }

        return result;
    }

    private string GetCacheKey(string text)
    {
        return $"embedding:{_options.Model}:{_options.Dimension}:{text}";
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyCollection<string> Input,
        [property: JsonPropertyName("dimensions")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            int? Dimensions
    );

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingItem> Data
    );

    private sealed record EmbeddingItem(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding
    );
}
