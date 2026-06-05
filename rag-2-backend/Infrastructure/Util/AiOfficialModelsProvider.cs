using Microsoft.Extensions.Caching.Memory;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

namespace rag_2_backend.Infrastructure.Util;

public class AiOfficialModelsProvider(
    IRag2AiModelsClient rag2AiModelsClient,
    IConfiguration configuration,
    IMemoryCache memoryCache
) : IAiOfficialModelsProvider
{
    private const string RoutesCacheKey = "rag2ai:routes";
    private const string OfficialNamesCacheKey = "rag2ai:official-names";

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

        var officialNames = GetOfficialNames();
        return officialNames.FirstOrDefault(m => string.Equals(m, modelName, StringComparison.OrdinalIgnoreCase));
    }

    private HashSet<string> GetOfficialNames()
    {
        if (memoryCache.TryGetValue(OfficialNamesCacheKey, out HashSet<string>? cached) && cached != null)
            return cached;

        return Rag2AiRouteParser.BuildOfficialModelNames([], GetFallbackModels());
    }

    private async Task<IReadOnlyList<Rag2AiRouteInfo>> GetRoutesCachedAsync(CancellationToken cancellationToken)
    {
        if (memoryCache.TryGetValue(RoutesCacheKey, out IReadOnlyList<Rag2AiRouteInfo>? cached) && cached != null)
            return cached;

        var routes = await rag2AiModelsClient.GetRoutesAsync(cancellationToken);
        var officialNames = Rag2AiRouteParser.BuildOfficialModelNames(routes, GetFallbackModels());
        var ttl = GetCacheTtl();

        memoryCache.Set(RoutesCacheKey, routes, ttl);
        memoryCache.Set(OfficialNamesCacheKey, officialNames, ttl);
        return routes;
    }

    private IEnumerable<string> GetFallbackModels() =>
        configuration.GetSection("Rag2Ai:FallbackOfficialModels").Get<string[]>() ?? [];

    private TimeSpan GetCacheTtl()
    {
        var minutes = configuration.GetValue("Rag2Ai:CacheMinutes", 10);
        return TimeSpan.FromMinutes(Math.Max(1, minutes));
    }
}
