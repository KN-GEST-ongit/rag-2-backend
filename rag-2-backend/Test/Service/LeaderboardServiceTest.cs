#region

using Moq;
using Newtonsoft.Json;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Database.Entity;
using rag_2_backend.Infrastructure.Module.Leaderboard;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;
using StackExchange.Redis;
using Xunit;

#endregion

namespace rag_2_backend.Test.Service;

public class LeaderboardServiceTests
{
    private readonly Mock<GameDao> _gameDaoMock = new(null!);
    private readonly Mock<GameScoreConfigDao> _gameScoreConfigDaoMock = new(null!);
    private readonly Mock<LeaderboardDao> _leaderboardDaoMock;
    private readonly Mock<IDatabase> _redisDatabaseMock = new();
    private readonly Mock<IAiOfficialModelsProvider> _aiOfficialModelsProviderMock = new();
    private readonly LeaderboardUtil _leaderboardUtil;
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardServiceTests()
    {
        Mock<IConnectionMultiplexer> redisMock = new();
        Mock<IConfiguration> configurationMock = new();
        configurationMock.Setup(c => c["Redis:Leaderboard:Prefix"]).Returns("Leaderboard:");
        configurationMock.Setup(c => c["Redis:Leaderboard:TtlDays"]).Returns("1");

        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        _leaderboardUtil = new LeaderboardUtil(
            configurationMock.Object,
            redisMock.Object,
            _aiOfficialModelsProviderMock.Object
        );
        _leaderboardDaoMock = new Mock<LeaderboardDao>(null!, _leaderboardUtil);
        _leaderboardService = new LeaderboardService(
            _gameDaoMock.Object,
            _gameScoreConfigDaoMock.Object,
            _leaderboardDaoMock.Object,
            _leaderboardUtil,
            _aiOfficialModelsProviderMock.Object
        );
    }

    [Fact]
    public async Task GetLeaderboard_Human_ShouldReturnEntries()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig { Game = game, GameId = gameId, ScoreType = ScoreType.Integer };
        var entries = new List<LeaderboardEntryResponse>
        {
            new() { Rank = 1, Score = 100, Name = "Bob", ControlSource = ControlSource.Human }
        };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetLeaderboardEntries(gameId, ScoreType.Integer, ControlSource.Human, null, 100))
            .ReturnsAsync(entries);

        var result = await _leaderboardService.GetLeaderboard(gameId, null, ControlSource.Human, null, null);

        Assert.Single(result);
        Assert.Equal("Bob", result[0].Name);
        Assert.Equal(ControlSource.Human, result[0].ControlSource);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldCapLimitAt100()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig { Game = game, GameId = gameId, ScoreType = ScoreType.Integer };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetLeaderboardEntries(gameId, ScoreType.Integer, null, null, 100))
            .ReturnsAsync([]);

        await _leaderboardService.GetLeaderboard(gameId, null, null, null, 500);

        _leaderboardDaoMock.Verify(d =>
            d.GetLeaderboardEntries(gameId, ScoreType.Integer, null, null, 100));
    }

    [Fact]
    public async Task GetLeaderboard_ShouldUseCache_WhenAvailable()
    {
        const int gameId = 99;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig { Game = game, GameId = gameId, ScoreType = ScoreType.Integer };
        var cached = Enumerable.Range(1, 20)
            .Select(i => new LeaderboardEntryResponse
            {
                Rank = i,
                Score = 100 - i,
                Name = $"User{i}",
                ControlSource = ControlSource.Human
            })
            .ToList();

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _redisDatabaseMock
            .Setup(d => d.StringGet(It.Is<RedisKey>(k => k == (RedisKey)"Leaderboard:99:all:all"), It.IsAny<CommandFlags>()))
            .Returns(JsonConvert.SerializeObject(cached));

        var result = await _leaderboardService.GetLeaderboard(gameId, null, null, null, 10);

        Assert.Equal(10, result.Count);
        _leaderboardDaoMock.Verify(
            d => d.GetLeaderboardEntries(It.IsAny<int>(), It.IsAny<ScoreType>(),
                It.IsAny<ControlSource?>(), It.IsAny<string?>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetLeaderboard_WithUserId_ShouldReturnCorrectRank()
    {
        const int gameId = 1;
        const int userId = 2;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig { Game = game, GameId = gameId, ScoreType = ScoreType.Integer };
        var allEntries = new List<LeaderboardEntryResponse>
        {
            new() { Rank = 1, Score = 100, Name = "Alice", ControlSource = ControlSource.Human, UserId = 1 },
            new() { Rank = 2, Score = 50, Name = "Bob", ControlSource = ControlSource.Human, UserId = userId }
        };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetLeaderboardEntries(gameId, ScoreType.Integer, null, null, int.MaxValue))
            .ReturnsAsync(allEntries);

        var result = await _leaderboardService.GetLeaderboard(gameId, userId, null, null, null);

        Assert.Single(result);
        Assert.Equal(2, result[0].Rank);
        Assert.Equal("Bob", result[0].Name);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldThrowBadRequest_WhenModelNameWithoutAiControlSource()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);

        await Assert.ThrowsAsync<HttpExceptions.Exceptions.BadRequestException>(() =>
            _leaderboardService.GetLeaderboard(gameId, null, null, "some-model", null));
    }

    [Fact]
    public async Task GetLeaderboard_ShouldApplyOffsetPagination()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig { Game = game, GameId = gameId, ScoreType = ScoreType.Integer };
        var entries = Enumerable.Range(1, 5)
            .Select(i => new LeaderboardEntryResponse
            {
                Rank = i, Score = 100 - i, Name = $"User{i}", ControlSource = ControlSource.Human
            }).ToList();

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetLeaderboardEntries(gameId, ScoreType.Integer, null, null, 100))
            .ReturnsAsync(entries);

        var result = await _leaderboardService.GetLeaderboard(gameId, null, null, null, 2, 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("User3", result[0].Name);
        Assert.Equal("User4", result[1].Name);
    }

    [Fact]
    public async Task GetAvailableModels_ShouldMergeAiServiceAndDatabaseModels()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _aiOfficialModelsProviderMock
            .Setup(p => p.GetModelsForGameAsync("flappybird", It.IsAny<CancellationToken>()))
            .ReturnsAsync(["flappybird-ppo", "flappybird-ars"]);
        _leaderboardDaoMock
            .Setup(d => d.GetAvailableModels(gameId))
            .ReturnsAsync(["flappybird-trpo"]);

        var result = await _leaderboardService.GetAvailableModels(gameId);

        Assert.Equal(3, result.Count);
        Assert.Contains("flappybird-ppo", result);
        Assert.Contains("flappybird-ars", result);
        Assert.Contains("flappybird-trpo", result);
    }

    [Fact]
    public async Task GetLeaderboard_Ai_ShouldUseModelSpecificCacheKey()
    {
        const int gameId = 2;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig { Game = game, GameId = gameId, ScoreType = ScoreType.Integer };
        var entries = new List<LeaderboardEntryResponse>
        {
            new() { Rank = 1, Score = 95, Name = "flappybird-ppo", ControlSource = ControlSource.AI }
        };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetLeaderboardEntries(gameId, ScoreType.Integer, ControlSource.AI, "flappybird-ppo", 100))
            .ReturnsAsync(entries);

        var result = await _leaderboardService.GetLeaderboard(gameId, null, ControlSource.AI, "flappybird-ppo", null);

        Assert.Single(result);
        Assert.Equal("flappybird-ppo", result[0].Name);

        _redisDatabaseMock.Verify(d =>
            d.StringSet(
                It.Is<RedisKey>(k => k == (RedisKey)"Leaderboard:2:AI:flappybird-ppo"),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldNotCache_WhenResultIsEmpty()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig { Game = game, GameId = gameId, ScoreType = ScoreType.Integer };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetLeaderboardEntries(gameId, ScoreType.Integer, null, null, 100))
            .ReturnsAsync([]);

        var result = await _leaderboardService.GetLeaderboard(gameId, null, null, null, null);

        Assert.Empty(result);
        _redisDatabaseMock.Verify(d =>
            d.StringSet(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()),
            Times.Never);
    }
}
