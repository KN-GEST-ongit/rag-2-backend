namespace rag_2_backend.Infrastructure.Util;

public interface IAiOfficialModelsProvider
{
    Task<IReadOnlyList<string>> GetModelsForGameAsync(string gameName, CancellationToken cancellationToken = default);

    string? ResolveCanonicalModelName(string? modelName);
}
