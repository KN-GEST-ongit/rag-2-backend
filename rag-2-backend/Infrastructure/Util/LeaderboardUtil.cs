#region

using Newtonsoft.Json;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using StackExchange.Redis;

#endregion

namespace rag_2_backend.Infrastructure.Util;

public class LeaderboardUtil(
    IConfiguration configuration,
    IConnectionMultiplexer redisConnection,
    IAiOfficialModelsProvider aiOfficialModelsProvider
)
{
    private readonly IDatabase _redisDatabase = redisConnection.GetDatabase();

    public string? ResolveModelName(string? modelName) =>
        aiOfficialModelsProvider.ResolveCanonicalModelName(modelName);

    public string GetCacheKey(int gameId, ControlSource? controlSource, string? modelName)
    {
        var cs = controlSource.HasValue ? controlSource.Value.ToString() : "all";
        var mn = modelName ?? "all";
        return $"{configuration["Redis:Leaderboard:Prefix"] ?? "Leaderboard:"}{gameId}:{cs}:{mn}";
    }

    public List<LeaderboardEntryResponse>? TryGetCached(int gameId, ControlSource? controlSource, string? modelName)
    {
        var cachedJson = _redisDatabase.StringGet(GetCacheKey(gameId, controlSource, modelName));
        return string.IsNullOrEmpty(cachedJson)
            ? null
            : JsonConvert.DeserializeObject<List<LeaderboardEntryResponse>>(cachedJson!);
    }

    public void SetCached(int gameId, ControlSource? controlSource, string? modelName, List<LeaderboardEntryResponse> entries)
    {
        var ttlDays = int.TryParse(configuration["Redis:Leaderboard:TtlDays"], out var days) ? days : 1;
        _redisDatabase.StringSet(
            GetCacheKey(gameId, controlSource, modelName),
            JsonConvert.SerializeObject(entries),
            TimeSpan.FromDays(ttlDays)
        );
    }

    public void InvalidateAll(int gameId, ControlSource controlSource, string? modelName)
    {
        _redisDatabase.KeyDelete(GetCacheKey(gameId, null, null));
        _redisDatabase.KeyDelete(GetCacheKey(gameId, controlSource, null));
        if (controlSource == ControlSource.AI && modelName != null)
            _redisDatabase.KeyDelete(GetCacheKey(gameId, controlSource, modelName));
    }
}
