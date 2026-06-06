#region

using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Module.GameRecord.Dto;
using rag_2_backend.Infrastructure.Util;
using Xunit;

#endregion

namespace rag_2_backend.Test.Util;

public class GameRecordScoreUtilTest
{
    [Fact]
    public void Resolve_ShouldUseDefaults_WhenNoScoreInState()
    {
        var request = new GameRecordRequest
        {
            GameName = "flappybird",
            OutputSpec = "spec",
            Players = [],
            Values = []
        };

        var (primaryScore, controlSource, modelName) = GameRecordScoreUtil.Resolve(request);

        Assert.Null(primaryScore);
        Assert.Equal(ControlSource.Human, controlSource);
        Assert.Null(modelName);
    }

    [Fact]
    public void Resolve_ShouldExtractScoreFromLastState()
    {
        var request = new GameRecordRequest
        {
            GameName = "flappybird",
            OutputSpec = "spec",
            Players = [],
            Values =
            [
                new GameRecordValue
                {
                    State = new { score = 15 }
                }
            ]
        };

        var (primaryScore, controlSource, modelName) = GameRecordScoreUtil.Resolve(request);

        Assert.Equal(15, primaryScore);
        Assert.Equal(ControlSource.Human, controlSource);
        Assert.Null(modelName);
    }
}
