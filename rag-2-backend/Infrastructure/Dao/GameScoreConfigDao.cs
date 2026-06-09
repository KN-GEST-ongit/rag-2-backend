#region

using HttpExceptions.Exceptions;
using Microsoft.EntityFrameworkCore;
using rag_2_backend.Infrastructure.Database;
using rag_2_backend.Infrastructure.Database.Entity;

#endregion

namespace rag_2_backend.Infrastructure.Dao;

public class GameScoreConfigDao(DatabaseContext dbContext)
{
    public virtual async Task<GameScoreConfig> GetByGameIdOrThrow(int gameId)
    {
        return await dbContext.GameScoreConfigs
                   .Include(c => c.Game)
                   .SingleOrDefaultAsync(c => c.GameId == gameId)
               ?? throw new NotFoundException("Game score config not found");
    }

    public virtual async Task<GameScoreConfig?> GetByGameId(int gameId)
    {
        return await dbContext.GameScoreConfigs
            .Include(c => c.Game)
            .SingleOrDefaultAsync(c => c.GameId == gameId);
    }
}
