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
    public async Task GetEndlessLeaderboardEntries_ShouldFilterByUserId()
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

        var result = await _leaderboardDao.GetEndlessLeaderboardEntries(
            gameId,
            1,
            ScoreType.Integer,
            10
        );

        Assert.Single(result);
        Assert.Equal(42, result[0].Score);
        Assert.Equal("Alice", result[0].UserName);
    }

    private static GameRecord CreateRecord(
        Game game,
        int userId,
        string userName,
        double score,
        ControlSource controlSource = ControlSource.Human
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
            PrimaryScore = score,
            IsEmptyRecord = false
        };
    }
}
