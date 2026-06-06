using Newtonsoft.Json;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;
using StackExchange.Redis;

namespace rag_2_backend.Infrastructure.Util;

public class AiOfficialModelsProvider(
    IRag2AiModelsClient rag2AiModelsClient,
    IConfiguration configuration,
    IConnectionMultiplexer redisConnection
) : IAiOfficialModelsProvider
{
    private readonly IDatabase _redisDatabase = redisConnection.GetDatabase();

    public async Task<IReadOnlyList<string>> GetModelsForGameAsync(
        string gameName,
        CancellationToken cancellationToken = default
    )
    {
        var routes = await GetRoutesCachedAsync(cancellationToken);
        return Rag2AiRouteParser.GetModelIdsForGame(gameName, routes)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? ResolveCanonicalModelName(string? modelName)
    {
        if (modelName == null)
            return null;

        var officialNames = GetOfficialNames(GetCachedRoutes());
        return officialNames.FirstOrDefault(m => string.Equals(m, modelName, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<Rag2AiRouteInfo>> GetRoutesCachedAsync(CancellationToken cancellationToken)
    {
        var cached = GetCachedRoutes();
        if (cached.Count > 0)
            return cached;

        var routes = await rag2AiModelsClient.GetRoutesAsync(cancellationToken);
        if (routes.Count > 0)
        {
            _redisDatabase.StringSet(
                GetRoutesCacheKey(),
                JsonConvert.SerializeObject(routes),
                GetCacheTtl()
            );
        }

        return routes;
    }

    private IReadOnlyList<Rag2AiRouteInfo> GetCachedRoutes()
    {
        var cachedJson = _redisDatabase.StringGet(GetRoutesCacheKey());
        if (string.IsNullOrEmpty(cachedJson))
            return [];

        return JsonConvert.DeserializeObject<List<Rag2AiRouteInfo>>(cachedJson!) ?? [];
    }

    private HashSet<string> GetOfficialNames(IReadOnlyList<Rag2AiRouteInfo> routes) =>
        Rag2AiRouteParser.BuildOfficialModelNames(routes, GetFallbackModels());

    private string GetRoutesCacheKey() =>
        $"{configuration["Redis:Rag2Ai:Prefix"] ?? "Rag2Ai:"}routes";

    private IEnumerable<string> GetFallbackModels() =>
        configuration.GetSection("Rag2Ai:FallbackOfficialModels").Get<string[]>() ?? [];

    private TimeSpan GetCacheTtl()
    {
        var minutes = configuration.GetValue("Rag2Ai:CacheMinutes", 10);
        return TimeSpan.FromMinutes(Math.Max(1, minutes));
    }
}
