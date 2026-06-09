using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

namespace rag_2_backend.Infrastructure.Util;

public static class Rag2AiRouteParser
{
    public static string NormalizePath(string path) => path.Trim().Trim('/');

    public static IEnumerable<string> GetModelIdsForGame(string gameName, IEnumerable<Rag2AiRouteInfo> routes)
    {
        var prefix = $"{gameName}-";
        foreach (var route in routes)
        {
            var pathId = NormalizePath(route.Path);
            if (pathId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                yield return pathId;

            if (!string.IsNullOrWhiteSpace(route.Name))
                yield return $"{gameName}-{route.Name.ToLowerInvariant()}";
        }
    }

    public static HashSet<string> BuildOfficialModelNames(
        IEnumerable<Rag2AiRouteInfo> routes,
        IEnumerable<string> fallbackModels
    )
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fallback in fallbackModels)
        {
            if (!string.IsNullOrWhiteSpace(fallback))
                names.Add(fallback.Trim());
        }

        foreach (var route in routes)
        {
            var pathId = NormalizePath(route.Path);
            if (!string.IsNullOrWhiteSpace(pathId))
                names.Add(pathId);

            if (!string.IsNullOrWhiteSpace(route.Name))
                names.Add(route.Name.Trim());
        }

        return names;
    }
}
