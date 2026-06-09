using System.Text.Json;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

namespace rag_2_backend.Infrastructure.Util;

public interface IRag2AiModelsClient
{
    Task<IReadOnlyList<Rag2AiRouteInfo>> GetRoutesAsync(CancellationToken cancellationToken = default);
}

public class Rag2AiModelsClient(HttpClient httpClient, IConfiguration configuration, ILogger<Rag2AiModelsClient> logger)
    : IRag2AiModelsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Rag2AiRouteInfo>> GetRoutesAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue("Rag2Ai:Enabled", true))
            return [];

        var baseUrl = configuration["Rag2Ai:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return [];

        var routeGames = configuration.GetSection("Rag2Ai:RouteGames").Get<string[]>();
        if (routeGames is { Length: > 0 })
        {
            var allRoutes = new List<Rag2AiRouteInfo>();
            foreach (var game in routeGames.Where(g => !string.IsNullOrWhiteSpace(g)))
            {
                var gameRoutes = await FetchRoutesAsync(
                    baseUrl,
                    $"ws/{game.Trim()}/routes/",
                    cancellationToken
                );
                allRoutes.AddRange(gameRoutes);
            }

            return allRoutes;
        }

        var routesPath = configuration["Rag2Ai:RoutesPath"] ?? "/";
        return await FetchRoutesAsync(baseUrl, routesPath.TrimStart('/'), cancellationToken);
    }

    private async Task<IReadOnlyList<Rag2AiRouteInfo>> FetchRoutesAsync(
        string baseUrl,
        string path,
        CancellationToken cancellationToken
    )
    {
        var requestUri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), path);

        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "rag-2-ai routes request failed with status {StatusCode} for {Url}",
                    response.StatusCode,
                    requestUri
                );
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var routes = await JsonSerializer.DeserializeAsync<List<Rag2AiRouteInfo>>(
                stream,
                JsonOptions,
                cancellationToken
            );
            return routes ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Failed to fetch model routes from rag-2-ai at {Url}", requestUri);
            return [];
        }
    }
}
