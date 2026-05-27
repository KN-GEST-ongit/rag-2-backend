#region

using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;

#endregion

namespace rag_2_backend.Infrastructure.Module.Leaderboard;

public class LeaderboardService(
    GameDao gameDao,
    GameScoreConfigDao gameScoreConfigDao,
    LeaderboardDao leaderboardDao,
    LeaderboardUtil leaderboardUtil
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

        var effectiveLimit = NormalizeLimit(limit);

        if (userId.HasValue)
        {
            return await leaderboardDao.GetEndlessLeaderboardEntries(
                gameId,
                userId,
                scoreConfig.ScoreType,
                effectiveLimit
            );
        }

        var cached = leaderboardUtil.TryGetCached(gameId);
        if (cached != null)
            return cached.Take(effectiveLimit).ToList();

        var entries = await leaderboardDao.GetEndlessLeaderboardEntries(
            gameId,
            null,
            scoreConfig.ScoreType,
            MaxLimit
        );

        leaderboardUtil.SetCached(gameId, entries);

        return entries.Take(effectiveLimit).ToList();
    }

    private static int NormalizeLimit(int? limit)
    {
        if (limit is null or <= 0)
            return DefaultLimit;

        return Math.Min(limit.Value, MaxLimit);
    }
}
