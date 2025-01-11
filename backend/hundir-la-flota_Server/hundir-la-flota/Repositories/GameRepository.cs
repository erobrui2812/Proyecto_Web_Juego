using hundir_la_flota.Models;

namespace hundir_la_flota.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly List<Game> _games = new List<Game>();

        public async Task AddAsync(Game game)
        {
            _games.Add(game);
        }

        public async Task<Game> GetByIdAsync(Guid gameId)
        {
            // Aquí buscamos usando GameId
            return _games.FirstOrDefault(g => g.GameId == gameId);
        }

        public async Task UpdateAsync(Game game)
        {
            var index = _games.FindIndex(g => g.GameId == game.GameId);
            if (index != -1)
            {
                _games[index] = game;
            }
        }
    }
}
