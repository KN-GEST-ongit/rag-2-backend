#region

using HttpExceptions.Exceptions;
using Moq;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Database.Entity;
using rag_2_backend.Infrastructure.Module.Leaderboard;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using Xunit;

#endregion

namespace rag_2_backend.Test.Service;

public class LeaderboardServiceTests
{
    private readonly Mock<GameDao> _gameDaoMock = new(null!);
    private readonly Mock<GameScoreConfigDao> _gameScoreConfigDaoMock = new(null!);
    private readonly Mock<LeaderboardDao> _leaderboardDaoMock = new(null!);
    private readonly LeaderboardService _leaderboardService;

    public LeaderboardServiceTests()
    {
        _leaderboardService = new LeaderboardService(
            _gameDaoMock.Object,
            _gameScoreConfigDaoMock.Object,
            _leaderboardDaoMock.Object
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
            ScoreType = ScoreType.Integer,
            GameType = GameType.Endless
        };
        var entries = new List<LeaderboardEntryResponse>
        {
            new() { Rank = 1, Score = 100, UserId = 2, UserName = "Bob" }
        };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);
        _leaderboardDaoMock
            .Setup(d => d.GetEndlessLeaderboardEntries(gameId, null, ScoreType.Integer, 50))
            .ReturnsAsync(entries);

        var result = await _leaderboardService.GetLeaderboard(gameId, null, null);

        Assert.Single(result);
        Assert.Equal("Bob", result[0].UserName);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldThrow_ForPongGame()
    {
        const int gameId = 2;
        var game = new Game { Id = gameId, Name = "pong" };
        var config = new GameScoreConfig
        {
            Game = game,
            GameId = gameId,
            ScoreType = ScoreType.Integer,
            GameType = GameType.Pong
        };

        _gameDaoMock.Setup(d => d.GetGameByIdOrThrow(gameId)).ReturnsAsync(game);
        _gameScoreConfigDaoMock.Setup(d => d.GetByGameIdOrThrow(gameId)).ReturnsAsync(config);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _leaderboardService.GetLeaderboard(gameId, null, null)
        );
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
            ScoreType = ScoreType.Integer,
            GameType = GameType.Endless
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
}
