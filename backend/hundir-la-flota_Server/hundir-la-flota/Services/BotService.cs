using System;
using System.Linq;
using System.Threading.Tasks;
using hundir_la_flota.Models;

namespace hundir_la_flota.Services
{
    public interface IBotService
    {
        Task<GameAction> ExecuteBotMove(Game game);
    }

    public class BotService : IBotService
    {
        public async Task<GameAction> ExecuteBotMove(Game game)
        {
            await Task.Delay(500);
            var board = game.Player1Board;
            var availableCells = board.Grid.Values.Where(cell => !cell.IsHit).ToList();
            if (!availableCells.Any())
            {
                throw new InvalidOperationException("No hay celdas disponibles para el ataque.");
            }
            var random = new Random();
            var targetCell = availableCells[random.Next(availableCells.Count)];

            targetCell.IsHit = true;
            targetCell.Status = targetCell.HasShip ? CellStatus.Hit : CellStatus.Miss;
            string details = $"El bot dispara en ({targetCell.X}, {targetCell.Y})";
            var hitShip = board.Ships.FirstOrDefault(ship => ship.Coordinates.Any(coord => coord.X == targetCell.X && coord.Y == targetCell.Y));
            if (hitShip != null)
            {
                foreach (var coord in hitShip.Coordinates)
                {
                    if (coord.X == targetCell.X && coord.Y == targetCell.Y)
                    {
                        coord.IsHit = true;
                        break;
                    }
                }
                if (hitShip.IsSunk)
                {
                    details += " ¡Barco hundido!";

                }
                else
                {
                    details += " ¡Acierto!";

                }
            }
            else
            {
                details += " ¡Fallo!";
                Console.WriteLine("BotService: Missed attack.");
            }
            Console.WriteLine($"BotService: Attack details: {details}");
            return new GameAction
            {
                PlayerId = 0,
                ActionType = "BotAttack",
                Timestamp = DateTime.UtcNow,
                Details = details,
                X = targetCell.X,
                Y = targetCell.Y
            };
        }
    }
}
