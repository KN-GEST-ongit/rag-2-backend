#region

using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Database;
using rag_2_backend.Infrastructure.Database.Entity;
using Xunit;

#endregion

namespace rag_2_backend.Test.Dao;

public class LeaderboardDaoTests
{
    private readonly Mock<DatabaseContext> _dbContextMock = new(
        new DbContextOptionsBuilder<DatabaseContext>().Options
    );

    private readonly LeaderboardDao _leaderboardDao;

    public LeaderboardDaoTests()
    {
        _leaderboardDao = new LeaderboardDao(_dbContextMock.Object);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Human_ShouldReturnAllScoresOrderedDesc()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "crossyroad" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 10),
            CreateRecord(game, 1, "Alice", 42),
            CreateRecord(game, 2, "Bob", 99)
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, ControlSource.Human, null, 10
        );

        Assert.Equal(3, result.Count);
        Assert.Equal(99, result[0].Score);
        Assert.Equal("Bob", result[0].Name);
        Assert.Equal(42, result[1].Score);
        Assert.Equal("Alice", result[1].Name);
        Assert.Equal(10, result[2].Score);
        Assert.Equal("Alice", result[2].Name);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Human_ShouldExcludeAiRecords()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "crossyroad" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 100),
            CreateRecord(game, 2, "Bob", 50, ControlSource.AI, "crossyroad-ppo")
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, ControlSource.Human, null, 10
        );

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Ai_ShouldShowModelName()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 75, ControlSource.AI, "flappybird-ppo"),
            CreateRecord(game, 2, "Bob", 60, ControlSource.AI, "flappybird-ppo")
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, ControlSource.AI, null, 10
        );

        Assert.Equal(2, result.Count);
        Assert.Equal("flappybird-ppo", result[0].Name);
        Assert.Equal(ControlSource.AI, result[0].ControlSource);
        Assert.Equal(75, result[0].Score);
        Assert.Equal(60, result[1].Score);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Ai_ShouldShowOwnerName_WhenCustomModelName()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 50, ControlSource.AI, "my-local-bot")
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, ControlSource.AI, null, 10
        );

        Assert.Single(result);
        Assert.Equal("Alice (custom model)", result[0].Name);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Decimal_ShouldIncludeDecimalScores()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "ballfall" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 499.65),
            CreateRecord(game, 2, "Bob", 300.0)
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Decimal, null, null, 10
        );

        Assert.Equal(2, result.Count);
        Assert.Equal(499.65, result[0].Score);
        Assert.Equal(300.0, result[1].Score);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Integer_ShouldExcludeDecimalScores()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "crossyroad" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 10.0),
            CreateRecord(game, 2, "Bob", 9.5)
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, null, null, 10
        );

        Assert.Single(result);
        Assert.Equal(10.0, result[0].Score);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Combined_ShouldIncludeHumanAndAi()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 100),
            CreateRecord(game, 2, "Bob", 80, ControlSource.AI, "flappybird-ppo")
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, null, null, 10
        );

        Assert.Equal(2, result.Count);
        Assert.Equal(1, result[0].Rank);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(ControlSource.Human, result[0].ControlSource);
        Assert.Equal(2, result[1].Rank);
        Assert.Equal("flappybird-ppo", result[1].Name);
        Assert.Equal(ControlSource.AI, result[1].ControlSource);
    }

    [Fact]
    public async Task GetLeaderboardEntries_Ai_ShouldFilterByModelName()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 90, ControlSource.AI, "flappybird-ppo"),
            CreateRecord(game, 2, "Bob", 70, ControlSource.AI, "flappybird-trpo")
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, ControlSource.AI, "flappybird-ppo", 10
        );

        Assert.Single(result);
        Assert.Equal("flappybird-ppo", result[0].Name);
        Assert.Equal(90, result[0].Score);
    }

    [Fact]
    public async Task GetLeaderboardEntries_ShouldExcludeRecordsWithNullScore()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 50),
            new GameRecord
            {
                GameId = gameId, Game = game, UserId = 2,
                User = new User { Id = 2, Name = "Bob", Email = "bob@test.com", Password = "pass" },
                Values = [], ControlSource = ControlSource.Human, PrimaryScore = null, IsEmptyRecord = true
            }
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetLeaderboardEntries(
            gameId, ScoreType.Integer, null, null, 10
        );

        Assert.Single(result);
        Assert.Equal("Alice", result[0].Name);
    }

    [Fact]
    public async Task GetAvailableModels_ShouldReturnDistinctAiModels()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var records = new List<GameRecord>
        {
            CreateRecord(game, 1, "Alice", 90, ControlSource.AI, "flappybird-ppo"),
            CreateRecord(game, 2, "Bob", 70, ControlSource.AI, "flappybird-ppo"),
            CreateRecord(game, 3, "Carol", 60, ControlSource.AI, "flappybird-trpo"),
            CreateRecord(game, 4, "Dave", 50)
        };

        _dbContextMock.Setup(db => db.GameRecords).ReturnsDbSet(records);

        var result = await _leaderboardDao.GetAvailableModels(gameId);

        Assert.Equal(2, result.Count);
        Assert.Contains("flappybird-ppo", result);
        Assert.Contains("flappybird-trpo", result);
    }

    private static GameRecord CreateRecord(
        Game game,
        int userId,
        string userName,
        double score,
        ControlSource controlSource = ControlSource.Human,
        string? modelName = null
    )
    {
        return new GameRecord
        {
            GameId = game.Id,
            Game = game,
            UserId = userId,
            User = new User
            {
                Id = userId,
                Name = userName,
                Email = $"{userName.ToLower()}@test.com",
                Password = "pass"
            },
            Values = [],
            ControlSource = controlSource,
            ModelName = modelName,
            PrimaryScore = score,
            IsEmptyRecord = false
        };
    }
}
