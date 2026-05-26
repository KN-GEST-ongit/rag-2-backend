#region

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

#endregion

namespace rag_2_backend.Infrastructure.Database;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new DatabaseContext(optionsBuilder.Options)
        {
            Games = null!,
            GameScoreConfigs = null!,
            GameRecords = null!,
            Users = null!,
            AccountConfirmationTokens = null!,
            RefreshTokens = null!,
            PasswordResetTokens = null!,
            Courses = null!
        };
    }
}
