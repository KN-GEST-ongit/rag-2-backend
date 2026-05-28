using System.Text.Json.Serialization;
using rag_2_backend.Infrastructure.Common.Model;

namespace rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

public class LeaderboardEntryResponse
{
    public int Rank { get; set; }
    public double Score { get; init; }
    public string Name { get; init; } = "";
    public ControlSource ControlSource { get; init; }
    [JsonIgnore] public int? UserId { get; init; }
}
