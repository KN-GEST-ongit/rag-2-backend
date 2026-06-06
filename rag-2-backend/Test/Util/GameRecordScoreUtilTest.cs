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

    [Fact]
    public void Resolve_ShouldExtractCurrentScore_ForSnake()
    {
        var request = new GameRecordRequest
        {
            GameName = "snake",
            OutputSpec = "spec",
            Players = [],
            Values =
            [
                new GameRecordValue
                {
                    State = new { currentScore = 7, gridSize = 30 }
                }
            ]
        };

        var (primaryScore, _, _) = GameRecordScoreUtil.Resolve(request);

        Assert.Equal(7, primaryScore);
    }

    [Fact]
    public void Resolve_ShouldExtractScore0_ForTetris()
    {
        var request = new GameRecordRequest
        {
            GameName = "tetris",
            OutputSpec = "spec",
            Players = [],
            Values =
            [
                new GameRecordValue
                {
                    State = new { rows = 20, cols = 10, score0 = 500, level0 = 3 }
                }
            ]
        };

        var (primaryScore, _, _) = GameRecordScoreUtil.Resolve(request);

        Assert.Equal(500, primaryScore);
    }

    [Fact]
    public void Resolve_ShouldNotTreatBoardDimensionsAsScore()
    {
        var request = new GameRecordRequest
        {
            GameName = "tetris",
            OutputSpec = "spec",
            Players = [],
            Values =
            [
                new GameRecordValue
                {
                    State = new { rows = 20, cols = 10, level0 = 3 }
                }
            ]
        };

        var (primaryScore, _, _) = GameRecordScoreUtil.Resolve(request);

        Assert.Null(primaryScore);
    }

    [Fact]
    public void Resolve_ShouldExtractScore_ForBallfallAndTwozerofoureight()
    {
        foreach (var gameName in new[] { "ballfall", "twozerofoureight" })
        {
            var request = new GameRecordRequest
            {
                GameName = gameName,
                OutputSpec = "spec",
                Players = [],
                Values =
                [
                    new GameRecordValue
                    {
                        State = new { score = 128, bestScore = 512, size = 4 }
                    }
                ]
            };

            var (primaryScore, _, _) = GameRecordScoreUtil.Resolve(request);

            Assert.Equal(128, primaryScore);
        }
    }

    [Fact]
    public void Resolve_ShouldExtractScoreP1_ForTimberman()
    {
        var request = new GameRecordRequest
        {
            GameName = "timberman",
            OutputSpec = "spec",
            Players = [],
            Values =
            [
                new GameRecordValue
                {
                    State = new { scoreP1 = 34, scoreP2 = 12, levelP1 = 2, timeLeftP1 = 45.5 }
                }
            ]
        };

        var (primaryScore, _, _) = GameRecordScoreUtil.Resolve(request);

        Assert.Equal(34, primaryScore);
    }
}
