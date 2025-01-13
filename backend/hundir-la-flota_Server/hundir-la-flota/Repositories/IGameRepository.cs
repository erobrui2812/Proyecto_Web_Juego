using hundir_la_flota.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGameRepository
{
    Task<List<Game>> GetAllAsync();
    Task<Game> GetByIdAsync(Guid gameId);
    Task AddAsync(Game game);
    Task UpdateAsync(Game game);
}
