#region

using rag_2_backend.Infrastructure.Util;
using Xunit;

#endregion

namespace rag_2_backend.Test.Util;

public class LeaderboardUtilTest
{
    [Fact]
    public void ResolveModelName_ShouldReturnName_WhenOfficial()
    {
        Assert.Equal("flappybird-ppo", LeaderboardUtil.ResolveModelName("flappybird-ppo"));
    }

    [Fact]
    public void ResolveModelName_ShouldReturnCustomModel_WhenUnknown()
    {
        Assert.Equal("Custom model", LeaderboardUtil.ResolveModelName("my-local-bot"));
    }

    [Fact]
    public void ResolveModelName_ShouldReturnCustomModel_WhenNull()
    {
        Assert.Equal("Custom model", LeaderboardUtil.ResolveModelName(null));
    }

    [Fact]
    public void ResolveModelName_ShouldBeCaseInsensitive()
    {
        Assert.Equal("flappybird-ppo", LeaderboardUtil.ResolveModelName("FlappyBird-PPO"));
    }
}
