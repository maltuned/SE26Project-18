using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SE26Project_18.Api.Infrastructure.Embedding;

namespace SE26Project_18.Api.Tests.Infrastructure;

public sealed class OpenAiEmbeddingServiceTests
{
    [Fact]
    public async Task EmbedAsync_PreservesBaseUrlPathWithoutTrailingSlash()
    {
        Uri? requestedUri = null;
        var handler = new StubHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":[{"index":0,"embedding":[1,0]}]}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            };
        });
        var service = new OpenAiEmbeddingService(
            new HttpClient(handler),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(
                new EmbeddingOptions
                {
                    BaseUrl = "https://example.test/v1",
                    ApiKey = "test-key",
                    Model = "test-model",
                    Dimension = 2,
                }
            )
        );

        var result = await service.EmbedAsync(["game tag: RPG"], CancellationToken.None);

        Assert.Equal("https://example.test/v1/embeddings", requestedUri?.ToString());
        Assert.Equal(2, result["game tag: RPG"].Length);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            return Task.FromResult(handler(request));
        }
    }
}
