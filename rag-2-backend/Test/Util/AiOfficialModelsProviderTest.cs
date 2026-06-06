#region

using Microsoft.Extensions.Configuration;
using Moq;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;
using StackExchange.Redis;
using Xunit;

#endregion

namespace rag_2_backend.Test.Util;

public class AiOfficialModelsProviderTest
{
    private readonly Mock<IDatabase> _redisDatabaseMock = new();

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

        _redisDatabaseMock
            .Setup(d => d.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(RedisValue.Null);

        var provider = CreateProvider(clientMock.Object);

        var result = await provider.GetModelsForGameAsync("flappybird");

        Assert.Contains("flappybird-ppo", result);
        Assert.Contains("flappybird-trpo", result);
    }

    [Fact]
    public void ResolveCanonicalModelName_ShouldUseFallbackWhenRoutesUnavailable()
    {
        var clientMock = new Mock<IRag2AiModelsClient>();
        clientMock.Setup(c => c.GetRoutesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        _redisDatabaseMock
            .Setup(d => d.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Returns(RedisValue.Null);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Rag2Ai:FallbackOfficialModels:0"] = "flappybird-ppo"
            })
            .Build();

        var provider = CreateProvider(clientMock.Object, configuration);

        Assert.Equal("flappybird-ppo", provider.ResolveCanonicalModelName("FlappyBird-PPO"));
        Assert.Null(provider.ResolveCanonicalModelName("my-local-bot"));
    }

    private AiOfficialModelsProvider CreateProvider(
        IRag2AiModelsClient client,
        IConfiguration? configuration = null
    )
    {
        configuration ??= new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Mock<IConnectionMultiplexer> redisMock = new();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        return new AiOfficialModelsProvider(client, configuration, redisMock.Object);
    }
}
