using System.Text.Json;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Module.GameRecord.Dto;

namespace rag_2_backend.Infrastructure.Util;

public static class GameRecordScoreUtil
{
    public const string BotModelName = "BOT";

    private static readonly string[] ScorePropertyNames =
    [
        "score", "currentScore", "score0", "score1", "scoreP1", "scoreP2",
        "points", "distance", "highScore", "level"
    ];

    public static (double? PrimaryScore, ControlSource ControlSource, string? ModelName) Resolve(
        GameRecordRequest request
    )
    {
        var primaryScore = TryExtractScoreFromValues(request.Values);
        var (controlSource, modelName) = ResolveControlSource(request.Players);
        return (primaryScore, controlSource, modelName);
    }

    private static (ControlSource ControlSource, string? ModelName) ResolveControlSource(
        List<Player> players
    )
    {
        var activePlayers = players.Where(p => p.IsActive).ToList();
        if (activePlayers.Count == 0)
            return (ControlSource.Human, null);

        var hasKeyboard = activePlayers.Any(p => p.PlayerType == PlayerType.Keyboard);
        var hasSocket = activePlayers.Any(p => p.PlayerType == PlayerType.Socket);

        if (hasSocket && !hasKeyboard)
            return (ControlSource.AI, ResolveSocketModelName(activePlayers));

        return (ControlSource.Human, null);
    }

    private static string ResolveSocketModelName(List<Player> activePlayers)
    {
        var modelName = activePlayers
            .Where(p => p.PlayerType == PlayerType.Socket)
            .Select(p => p.ModelName?.Trim())
            .FirstOrDefault(name => !string.IsNullOrEmpty(name));

        return string.IsNullOrEmpty(modelName) ? BotModelName : modelName;
    }

    private static double? TryExtractScoreFromValues(List<GameRecordValue> values)
    {
        if (values.Count == 0)
            return null;

        return TryExtractScoreFromState(values[^1].State);
    }

    private static double? TryExtractScoreFromState(object? state)
    {
        if (state == null)
            return null;

        try
        {
            var json = JsonSerializer.Serialize(state);
            using var document = JsonDocument.Parse(json);
            return TryExtractScoreFromElement(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static double? TryExtractScoreFromElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.GetDouble();
            case JsonValueKind.Object:
            {
                foreach (var propertyName in ScorePropertyNames)
                {
                    if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
                        continue;

                    var score = TryReadNumber(value);
                    if (score != null)
                        return score;
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind != JsonValueKind.Object)
                        continue;

                    var nestedScore = TryExtractScoreFromElement(property.Value);
                    if (nestedScore != null)
                        return nestedScore;
                }

                return null;
            }
            default:
                return null;
        }
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value
    )
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static double? TryReadNumber(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String when double.TryParse(
                element.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed
            ) => parsed,
            _ => null
        };
}
