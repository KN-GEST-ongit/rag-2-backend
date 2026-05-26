namespace rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

public class LeaderboardEntryResponse
{
    public int Rank { get; set; }
    public double Score { get; init; }
    public int UserId { get; init; }
    public string UserName { get; init; } = "";
}
