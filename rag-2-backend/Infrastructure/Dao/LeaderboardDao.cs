#region

using Microsoft.EntityFrameworkCore;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Database;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using rag_2_backend.Infrastructure.Util;

#endregion

namespace rag_2_backend.Infrastructure.Dao;

public class LeaderboardDao(DatabaseContext dbContext)
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

        var records = await query.ToListAsync();

        if (scoreType == ScoreType.Integer)
            records = records.Where(r => IsWholeNumber(r.PrimaryScore!.Value)).ToList();

        var entries = records
            .OrderByDescending(r => r.PrimaryScore!.Value)
            .Take(limit)
            .Select((r, i) => new LeaderboardEntryResponse
            {
                Name = r.ControlSource == ControlSource.Human
                    ? r.User.Name
                    : LeaderboardUtil.ResolveModelName(r.ModelName),
                ControlSource = r.ControlSource,
                Score = r.PrimaryScore!.Value,
                UserId = r.ControlSource == ControlSource.Human ? r.UserId : null
            })
            .ToList();

        for (var i = 0; i < entries.Count; i++)
            entries[i].Rank = i + 1;

        return entries;
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
