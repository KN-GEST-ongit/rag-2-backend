#region

using HttpExceptions.Exceptions;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

#endregion

namespace rag_2_backend.Infrastructure.Module.Leaderboard;

public class LeaderboardService(
    GameDao gameDao,
    GameScoreConfigDao gameScoreConfigDao,
    LeaderboardDao leaderboardDao
)
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;

    public async Task<List<LeaderboardEntryResponse>> GetLeaderboard(
        int gameId,
        int? userId,
        int? limit
    )
    {
        await gameDao.GetGameByIdOrThrow(gameId);
        var scoreConfig = await gameScoreConfigDao.GetByGameIdOrThrow(gameId);

        if (scoreConfig.GameType != GameType.Endless)
            throw new BadRequestException("Leaderboard is only available for endless games");

        var effectiveLimit = NormalizeLimit(limit);

        return await leaderboardDao.GetEndlessLeaderboardEntries(
            gameId,
            userId,
            scoreConfig.ScoreType,
            effectiveLimit
        );
    }

    private static int NormalizeLimit(int? limit)
    {
        if (limit is null or <= 0)
            return DefaultLimit;

        return Math.Min(limit.Value, MaxLimit);
    }
}
