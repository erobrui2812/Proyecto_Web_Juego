namespace hundir_la_flota.Repositories;
using hundir_la_flota.Models;

public interface IGameRepository
{
    Task AddAsync(Game game);
    Task<Game> GetByIdAsync(Guid gameId);
    Task UpdateAsync(Game game);
}

