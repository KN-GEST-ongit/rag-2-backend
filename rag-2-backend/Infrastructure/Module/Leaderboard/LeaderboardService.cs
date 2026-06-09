#region

using HttpExceptions.Exceptions;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Dao;
using rag_2_backend.Infrastructure.Module.Game.Dto;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;

#endregion

namespace rag_2_backend.Infrastructure.Module.Leaderboard;

public class LeaderboardService(
    GameDao gameDao,
    GameScoreConfigDao gameScoreConfigDao,
    LeaderboardDao leaderboardDao,
    LeaderboardUtil leaderboardUtil,
    IAiOfficialModelsProvider aiOfficialModelsProvider
)
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;

    public async Task<List<LeaderboardEntryResponse>> GetLeaderboard(
        int gameId,
        int? userId,
        ControlSource? controlSource,
        string? modelName,
        int? limit,
        int? offset = null
    )
    {
        if (modelName != null && controlSource != ControlSource.AI)
            throw new BadRequestException("modelName can only be used with controlSource=AI");

        await gameDao.GetGameByIdOrThrow(gameId);
        var scoreConfig = await gameScoreConfigDao.GetByGameIdOrThrow(gameId);

        var effectiveLimit = NormalizeLimit(limit);

        if (userId.HasValue)
        {
            var all = await leaderboardDao.GetLeaderboardEntries(
                gameId, scoreConfig.ScoreType, controlSource, modelName, int.MaxValue
            );
            var userEntry = all.FirstOrDefault(e => e.UserId == userId.Value);
            return userEntry != null ? [userEntry] : [];
        }

        var cached = leaderboardUtil.TryGetCached(gameId, controlSource, modelName);
        var entries = cached ?? await leaderboardDao.GetLeaderboardEntries(
            gameId, scoreConfig.ScoreType, controlSource, modelName, MaxLimit
        );

        if (cached == null && entries.Count > 0)
            leaderboardUtil.SetCached(gameId, controlSource, modelName, entries);

        var effectiveOffset = Math.Max(offset ?? 0, 0);
        return entries.Skip(effectiveOffset).Take(effectiveLimit).ToList();
    }

    public async Task<List<GameResponse>> GetGamesWithLeaderboard()
    {
        return await leaderboardDao.GetGamesWithLeaderboard();
    }

    public async Task<List<string>> GetAvailableModels(int gameId)
    {
        var game = await gameDao.GetGameByIdOrThrow(gameId);
        var fromAiService = await aiOfficialModelsProvider.GetModelsForGameAsync(game.Name);
        var fromDatabase = await leaderboardDao.GetAvailableModels(gameId);

        return fromAiService
            .Concat(fromDatabase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int NormalizeLimit(int? limit)
    {
        if (limit is null or <= 0)
            return DefaultLimit;

        return Math.Min(limit.Value, MaxLimit);
    }
}
