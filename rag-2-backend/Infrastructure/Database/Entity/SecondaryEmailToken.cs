#region

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#endregion

namespace rag_2_backend.Infrastructure.Database.Entity;

[Table("secondary_email_token_table")]
public class SecondaryEmailToken
{
    [Key] [MaxLength(100)] public required string Token { get; init; }
    public required DateTime Expiration { get; set; }
    [MaxLength(100)] public required string PendingEmail { get; set; }
    [ForeignKey("UserId")] public required User User { get; init; }
}
