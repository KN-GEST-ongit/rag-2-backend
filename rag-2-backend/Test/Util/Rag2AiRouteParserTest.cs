#region

using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;
using Xunit;

#endregion

namespace rag_2_backend.Test.Util;

public class Rag2AiRouteParserTest
{
    [Fact]
    public void GetModelIdsForGame_ShouldReturnGamePrefixedPaths()
    {
        var routes = new[]
        {
            new Rag2AiRouteInfo { Path = "/flappybird-ppo/", Name = "PPO" },
            new Rag2AiRouteInfo { Path = "/crossyroad-dqn/", Name = "DQN" }
        };

        var result = Rag2AiRouteParser.GetModelIdsForGame("flappybird", routes).ToList();

        Assert.Contains("flappybird-ppo", result);
        Assert.DoesNotContain("crossyroad-dqn", result);
    }

    [Fact]
    public void BuildOfficialModelNames_ShouldIncludePathAndFallback()
    {
        var routes = new[] { new Rag2AiRouteInfo { Path = "/flappybird-ppo/", Name = "PPO" } };
        var names = Rag2AiRouteParser.BuildOfficialModelNames(routes, ["flappybird-trpo"]);

        Assert.Contains("flappybird-ppo", names);
        Assert.Contains("PPO", names);
        Assert.Contains("flappybird-trpo", names);
    }

    [Fact]
    public void BuildOfficialModelNames_ShouldMatchCaseInsensitive()
    {
        var names = Rag2AiRouteParser.BuildOfficialModelNames([], ["flappybird-ppo"]);
        Assert.True(names.Contains("FlappyBird-PPO"));
    }
}
