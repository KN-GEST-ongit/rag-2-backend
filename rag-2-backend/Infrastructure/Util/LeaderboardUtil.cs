#region

using Newtonsoft.Json;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using StackExchange.Redis;

#endregion

namespace rag_2_backend.Infrastructure.Util;

public class LeaderboardUtil(IConfiguration configuration, IConnectionMultiplexer redisConnection)
{
    private readonly IDatabase _redisDatabase = redisConnection.GetDatabase();

    public string GetCacheKey(int gameId) =>
        $"{configuration["Redis:Leaderboard:Prefix"] ?? "Leaderboard:"}{gameId}";

    public List<LeaderboardEntryResponse>? TryGetCached(int gameId)
    {
        var cachedJson = _redisDatabase.StringGet(GetCacheKey(gameId));
        return string.IsNullOrEmpty(cachedJson)
            ? null
            : JsonConvert.DeserializeObject<List<LeaderboardEntryResponse>>(cachedJson!);
    }

    public void SetCached(int gameId, List<LeaderboardEntryResponse> entries)
    {
        _redisDatabase.StringSet(
            GetCacheKey(gameId),
            JsonConvert.SerializeObject(entries),
            TimeSpan.FromDays(1)
        );
    }

    public void Invalidate(int gameId)
    {
        _redisDatabase.KeyDelete(GetCacheKey(gameId));
    }
}
