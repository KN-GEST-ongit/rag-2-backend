#region

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;
using Xunit;

#endregion

namespace rag_2_backend.Test.Util;

public class AiOfficialModelsProviderTest
{
    [Fact]
    public async Task GetModelsForGameAsync_ShouldUseRoutesFromRag2AiClient()
    {
        var routes = new List<Rag2AiRouteInfo>
        {
            new() { Path = "/flappybird-ppo/", Name = "PPO" },
            new() { Path = "/flappybird-trpo/", Name = "TRPO" }
        };

        var clientMock = new Mock<IRag2AiModelsClient>();
        clientMock.Setup(c => c.GetRoutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(routes);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var provider = new AiOfficialModelsProvider(clientMock.Object, configuration, new MemoryCache(new MemoryCacheOptions()));

        var result = await provider.GetModelsForGameAsync("flappybird");

        Assert.Contains("flappybird-ppo", result);
        Assert.Contains("flappybird-trpo", result);
    }

    [Fact]
    public void ResolveCanonicalModelName_ShouldUseFallbackWhenRoutesUnavailable()
    {
        var clientMock = new Mock<IRag2AiModelsClient>();
        clientMock.Setup(c => c.GetRoutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Rag2Ai:FallbackOfficialModels:0"] = "flappybird-ppo"
            })
            .Build();

        var provider = new AiOfficialModelsProvider(clientMock.Object, configuration, new MemoryCache(new MemoryCacheOptions()));

        Assert.Equal("flappybird-ppo", provider.ResolveCanonicalModelName("FlappyBird-PPO"));
        Assert.Null(provider.ResolveCanonicalModelName("my-local-bot"));
    }
}
