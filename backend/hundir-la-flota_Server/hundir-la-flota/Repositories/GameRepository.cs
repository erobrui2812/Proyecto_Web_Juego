using hundir_la_flota.Models;

namespace hundir_la_flota.Repositories
{
    public class GameRepository : IGameRepository
    {
        private readonly List<Game> _games = new List<Game>(); // Almacén en memoria (puedes reemplazarlo por base de datos)

        public async Task AddAsync(Game game)
        {
            _games.Add(game);
        }

        public async Task<Game> GetByIdAsync(Guid gameId)
        {
            // Aquí buscamos usando GameId
            return _games.FirstOrDefault(g => g.GameId == gameId); // Corregido de g.Id a g.GameId
        }

        public async Task UpdateAsync(Game game)
        {
            var index = _games.FindIndex(g => g.GameId == game.GameId); // Corregido de g.Id a g.GameId
            if (index != -1)
            {
                _games[index] = game;
            }
        }
    }
}
