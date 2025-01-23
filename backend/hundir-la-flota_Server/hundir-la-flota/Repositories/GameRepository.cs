using hundir_la_flota.Models;
using Microsoft.EntityFrameworkCore;

public class GameRepository : IGameRepository
{
    private readonly MyDbContext _context;

    public GameRepository(MyDbContext context)
    {
        _context = context;
    }

    public async Task<List<Game>> GetAllAsync()
    {
        return await _context.Games
            .Include(g => g.Participants)
            .ToListAsync();
    }

    public async Task<Game> GetByIdAsync(Guid gameId)
    {
        return await _context.Games
            .Include(g => g.Participants)
            .FirstOrDefaultAsync(g => g.GameId == gameId);
    }

    public async Task AddAsync(Game game)
    {
        _context.Games.Add(game);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Game game)
    {
        _context.Games.Update(game);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Game>> GetGamesByUserIdAsync(int userId)
    {
        return await _context.Games
            .Include(g => g.Participants)
            .Where(g => g.Participants.Any(p => p.UserId == userId))
            .ToListAsync();
    }
}
