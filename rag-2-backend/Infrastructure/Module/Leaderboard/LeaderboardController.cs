#region

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

#endregion

namespace rag_2_backend.Infrastructure.Module.Leaderboard;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController(LeaderboardService leaderboardService) : ControllerBase
{
    /// <summary>
    /// Get endless game leaderboard (best Human scores per player).
    /// Available for crossyroad and flappybird.
    /// </summary>
    /// <response code="404">Game or score config not found</response>
    /// <response code="400">Game does not support leaderboard (e.g. pong)</response>
    [HttpGet]
    public async Task<List<LeaderboardEntryResponse>> GetLeaderboard(
        [Required] [FromQuery] int gameId,
        [FromQuery] int? userId,
        [FromQuery] int? limit
    )
    {
        return await leaderboardService.GetLeaderboard(gameId, userId, limit);
    }
}
