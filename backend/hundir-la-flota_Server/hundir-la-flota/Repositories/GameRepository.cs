using hundir_la_flota.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GameRepository : IGameRepository
{
    private readonly List<Game> _games = new List<Game>();

    public Task<List<Game>> GetAllAsync()
    {
        return Task.FromResult(_games);
    }

    public Task<Game> GetByIdAsync(Guid gameId)
    {
        return Task.FromResult(_games.FirstOrDefault(g => g.GameId == gameId));
    }

    public Task AddAsync(Game game)
    {
        _games.Add(game);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Game game)
    {
        var index = _games.FindIndex(g => g.GameId == game.GameId);
        if (index != -1)
        {
            _games[index] = game;
        }
        return Task.CompletedTask;
    }
}
