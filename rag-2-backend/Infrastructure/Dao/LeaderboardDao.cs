#region

using Microsoft.EntityFrameworkCore;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Database;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

#endregion

namespace rag_2_backend.Infrastructure.Dao;

public class LeaderboardDao(DatabaseContext dbContext)
{
    public virtual async Task<List<LeaderboardEntryResponse>> GetEndlessLeaderboardEntries(
        int gameId,
        int? userId,
        ScoreType scoreType,
        int limit
    )
    {
        var query = dbContext.GameRecords
            .AsNoTracking()
            .Include(r => r.User)
            .Where(r =>
                r.GameId == gameId &&
                !r.IsEmptyRecord &&
                r.PrimaryScore != null &&
                r.ControlSource == ControlSource.Human);

        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId);

        var records = await query.ToListAsync();

        if (scoreType == ScoreType.Integer)
            records = records.Where(r => IsWholeNumber(r.PrimaryScore!.Value)).ToList();

        var entries = records
            .GroupBy(r => r.UserId)
            .Select(group => new LeaderboardEntryResponse
            {
                UserId = group.Key,
                UserName = group.First().User.Name,
                Score = group.Max(r => r.PrimaryScore!.Value)
            })
            .OrderByDescending(e => e.Score)
            .Take(limit)
            .ToList();

        for (var i = 0; i < entries.Count; i++)
            entries[i].Rank = i + 1;

        return entries;
    }

    private static bool IsWholeNumber(double score) => Math.Abs(score % 1) < 0.000001;
}
