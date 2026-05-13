#region

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

#endregion

namespace rag_2_backend.Infrastructure.Database;

public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres");

        return new DatabaseContext(optionsBuilder.Options)
        {
            Games = null!,
            GameRecords = null!,
            Users = null!,
            AccountConfirmationTokens = null!,
            RefreshTokens = null!,
            PasswordResetTokens = null!,
            SecondaryEmailTokens = null!
        };
    }
}
