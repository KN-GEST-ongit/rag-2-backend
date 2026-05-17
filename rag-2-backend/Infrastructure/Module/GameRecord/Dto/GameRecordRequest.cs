#region

using rag_2_backend.Infrastructure.Common.Model;

#endregion

namespace rag_2_backend.Infrastructure.Module.GameRecord.Dto;

public class GameRecordRequest
{
    public required string GameName { get; init; }
    public required string OutputSpec { get; init; }
    public required List<GameRecordValue> Values { get; init; }
    public required List<Player> Players { get; init; }

    public double? PrimaryScore { get; init; }
    public ControlSource ControlSource { get; init; }
    public string? ModelName { get; init; }
}