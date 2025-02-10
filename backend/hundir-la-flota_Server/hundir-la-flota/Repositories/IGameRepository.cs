using hundir_la_flota.Models;


public interface IGameRepository
{
    Task<List<Game>> GetAllAsync();
    Task<Game> GetByIdAsync(Guid gameId);
    Task AddAsync(Game game);
    Task UpdateAsync(Game game);
    Task<List<Game>> GetGamesByUserIdAsync(int userId);
    Task RemoveAsync(Game game);
}