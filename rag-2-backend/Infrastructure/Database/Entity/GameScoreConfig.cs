#region

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using rag_2_backend.Infrastructure.Common.Model;

#endregion

namespace rag_2_backend.Infrastructure.Database.Entity;

[Table("game_score_config_table")]
[Index(nameof(GameId), IsUnique = true)]
public class GameScoreConfig
{
    [Key] public int Id { get; init; }

    public int GameId { get; init; }

    [ForeignKey(nameof(GameId))] public required Game Game { get; init; }

    public ScoreType ScoreType { get; set; }
}
