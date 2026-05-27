#region

using Moq;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Database.Entity;
using rag_2_backend.Infrastructure.Module.Leaderboard;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using Newtonsoft.Json;
using rag_2_backend.Infrastructure.Util;
using StackExchange.Redis;
using Xunit;

#endregion

namespace rag_2_backend.Test.Service;

public class LeaderboardServiceTests
{
    private readonly Mock<GameDao> _gameDaoMock = new(null!);
    private readonly Mock<GameScoreConfigDao> _gameScoreConfigDaoMock = new(null!);
    private readonly Mock<LeaderboardDao> _leaderboardDaoMock = new(null!);
    private readonly Mock<IDatabase> _redisDatabaseMock = new();
    private readonly LeaderboardUtil _leaderboardUtil;
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardServiceTests()
    {
        Mock<IConnectionMultiplexer> redisMock = new();
        Mock<IConfiguration> configurationMock = new();
        configurationMock.Setup(c => c["Redis:Leaderboard:Prefix"]).Returns("Leaderboard:");

        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_redisDatabaseMock.Object);

        _leaderboardUtil = new LeaderboardUtil(configurationMock.Object, redisMock.Object);
        _leaderboardService = new LeaderboardService(
            _gameDaoMock.Object,
            _gameScoreConfigDaoMock.Object,
            _leaderboardDaoMock.Object,
            _leaderboardUtil
        );
    }

    [Fact]
    public async Task GetLeaderboard_ShouldReturnEntries_ForEndlessGame()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig
        {
            Game = game,
            GameId = gameId,
            ScoreType = ScoreType.Integer
        };
        var entries = new List<LeaderboardEntryResponse>
        {
            new() { Rank = 1, Score = 100, UserId = 2, UserName = "Bob" }
        };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetEndlessLeaderboardEntries(gameId, null, ScoreType.Integer, 100))
            .ReturnsAsync(entries);

        var result = await _leaderboardService.GetLeaderboard(gameId, null, null);

        Assert.Single(result);
        Assert.Equal("Bob", result[0].UserName);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldCapLimitAt100()
    {
        const int gameId = 1;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig
        {
            Game = game,
            GameId = gameId,
            ScoreType = ScoreType.Integer
        };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetEndlessLeaderboardEntries(gameId, null, ScoreType.Integer, 100))
            .ReturnsAsync([]);

        await _leaderboardService.GetLeaderboard(gameId, null, 500);

        _leaderboardDaoMock.Verify(d =>
            d.GetEndlessLeaderboardEntries(gameId, null, ScoreType.Integer, 100));
    }

    [Fact]
    public async Task GetLeaderboard_ShouldUseCache_WhenAvailable()
    {
        const int gameId = 99;
        var game = new Game { Id = gameId, Name = "flappybird" };
        var config = new GameScoreConfig
        {
            Game = game,
            GameId = gameId,
            ScoreType = ScoreType.Integer
        };
        var cached = Enumerable.Range(1, 20)
            .Select(i => new LeaderboardEntryResponse
            {
                Rank = i,
                Score = 100 - i,
                UserId = i,
                UserName = $"User{i}"
            })
            .ToList();

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _redisDatabaseMock
            .Setup(d => d.StringGet(It.Is<RedisKey>(k => k == (RedisKey)"Leaderboard:99"), It.IsAny<CommandFlags>()))
            .Returns(JsonConvert.SerializeObject(cached));

        var result = await _leaderboardService.GetLeaderboard(gameId, null, 10);

        Assert.Equal(10, result.Count);
        _leaderboardDaoMock.Verify(
            d => d.GetEndlessLeaderboardEntries(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<ScoreType>(), It.IsAny<int>()),
            Times.Never);
    }
}
