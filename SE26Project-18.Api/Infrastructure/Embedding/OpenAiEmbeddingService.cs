using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace SE26Project_18.Api.Infrastructure.Embedding;

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
                result[text] = cached;
            else
                missingTexts.Add(text);
        }

        if (missingTexts.Count == 0)
            return result;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Embedding API key is not configured.");

        var normalizedBaseUrl = _options.BaseUrl.EndsWith('/')
            ? _options.BaseUrl
            : $"{_options.BaseUrl}/";
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(new Uri(normalizedBaseUrl), "embeddings")
        );
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(
            new EmbeddingRequest(_options.Model, missingTexts, _options.Dimension)
        );

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        var payload =
            await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Embedding API returned an empty response.");

        if (payload.Data.Count != missingTexts.Count)
            throw new InvalidOperationException(
                "Embedding API returned an unexpected result count."
            );

        foreach (var item in payload.Data)
        {
            if (item.Index < 0 || item.Index >= missingTexts.Count)
                throw new InvalidOperationException(
                    "Embedding API returned an invalid result index."
                );
            if (item.Embedding.Length != _options.Dimension)
                throw new InvalidOperationException(
                    $"Embedding API returned dimension {item.Embedding.Length}; expected {_options.Dimension}."
                );

            var text = missingTexts[item.Index];
            _cache.Set(GetCacheKey(text), item.Embedding, TimeSpan.FromHours(12));
            result[text] = item.Embedding;
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
        [property: JsonPropertyName("dimensions")] int Dimensions
    );

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<EmbeddingItem> Data
    );

    private sealed record EmbeddingItem(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding
    );
}
