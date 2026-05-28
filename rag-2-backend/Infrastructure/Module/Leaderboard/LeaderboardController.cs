#region

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using rag_2_backend.Infrastructure.Common.Model;
using rag_2_backend.Infrastructure.Module.Leaderboard.Dto;

#endregion

namespace rag_2_backend.Infrastructure.Module.Leaderboard;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController(LeaderboardService leaderboardService) : ControllerBase
{
    /// <summary>
    /// Get available AI model names for a game leaderboard.
    /// </summary>
    /// <response code="404">Game not found</response>
    [HttpGet("models")]
    public async Task<List<string>> GetAvailableModels([Required] [FromQuery] int gameId)
    {
        return await leaderboardService.GetAvailableModels(gameId);
    }

    /// <summary>
    /// Get game leaderboard. controlSource: Human | AI | omit for combined ranking.
    /// Available for crossyroad and flappybird.
    /// </summary>
    /// <response code="404">Game or score config not found</response>
    /// <response code="400">modelName can only be used with controlSource=AI</response>
    [HttpGet]
    public async Task<List<LeaderboardEntryResponse>> GetLeaderboard(
        [Required] [FromQuery] int gameId,
        [FromQuery] int? userId,
        [FromQuery] ControlSource? controlSource,
        [FromQuery] string? modelName,
        [FromQuery] int? limit,
        [FromQuery] int? offset
    )
    {
        return await leaderboardService.GetLeaderboard(gameId, userId, controlSource, modelName, limit, offset);
    }
}
