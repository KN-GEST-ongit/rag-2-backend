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
    public void ResolveModelName_ShouldReturnNull_WhenUnknown()
    {
        Assert.Null(LeaderboardUtil.ResolveModelName("my-local-bot"));
    }

    [Fact]
    public void ResolveModelName_ShouldReturnNull_WhenNull()
    {
        Assert.Null(LeaderboardUtil.ResolveModelName(null));
    }

    [Fact]
    public void ResolveModelName_ShouldBeCaseInsensitive()
    {
        Assert.Equal("flappybird-ppo", LeaderboardUtil.ResolveModelName("FlappyBird-PPO"));
    }
}
