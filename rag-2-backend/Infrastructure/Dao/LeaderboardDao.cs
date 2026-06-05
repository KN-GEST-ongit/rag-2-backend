#region

using Microsoft.EntityFrameworkCore;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Database;
using rag_2_backend.Infrastructure.Database.Entity;
using rag_2_backend.Infrastructure.Module.Game.Dto;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;

#endregion

namespace rag_2_backend.Infrastructure.Dao;

public class LeaderboardDao(DatabaseContext dbContext, LeaderboardUtil leaderboardUtil)
{
    public virtual async Task<List<LeaderboardEntryResponse>> GetLeaderboardEntries(
        int gameId,
        ScoreType scoreType,
        ControlSource? controlSource,
        string? modelName,
        int limit
    )
    {
        var query = dbContext.GameRecords
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r =>
                r.GameId == gameId &&
                r.PrimaryScore != null);

        if (controlSource.HasValue)
            query = query.Where(r => r.ControlSource == controlSource.Value);

        if (modelName != null)
            query = query.Where(r => r.ModelName == modelName);

        List<GameRecord> records;
        if (scoreType == ScoreType.Decimal)
        {
            records = await query
                .OrderByDescending(r => r.PrimaryScore!.Value)
                .Take(limit)
                .ToListAsync();
        }
        else
        {
            var all = await query.ToListAsync();
            records = all
                .Where(r => IsWholeNumber(r.PrimaryScore!.Value))
                .OrderByDescending(r => r.PrimaryScore!.Value)
                .Take(limit)
                .ToList();
        }

        return records
            .Select((r, i) => new LeaderboardEntryResponse
            {
                Rank = i + 1,
                Name = r.ControlSource == ControlSource.Human
                    ? r.User.Name
                    : leaderboardUtil.ResolveModelName(r.ModelName)
                      ?? $"{r.User.Name} (custom model)",
                ControlSource = r.ControlSource,
                Score = Math.Round(r.PrimaryScore!.Value, 2),
                UserId = r.ControlSource == ControlSource.Human ? r.UserId : null
            })
            .ToList();
    }

    public virtual async Task<List<GameResponse>> GetGamesWithLeaderboard()
    {
        return await dbContext.GameScoreConfigs
            .AsNoTracking()
            .Include(c => c.Game)
            .OrderBy(c => c.Game.Name)
            .Select(c => new GameResponse { Id = c.Game.Id, Name = c.Game.Name })
            .ToListAsync();
    }

    public virtual async Task<List<string>> GetAvailableModels(int gameId)
    {
        return await dbContext.GameRecords
            .AsNoTracking()
            .Where(r => r.GameId == gameId && r.ControlSource == ControlSource.AI && r.ModelName != null)
            .Select(r => r.ModelName!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
    }

    private static bool IsWholeNumber(double score) => Math.Abs(score % 1) < 0.000001;
}
