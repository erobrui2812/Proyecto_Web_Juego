using hundir_la_flota.DTOs;
using hundir_la_flota.Models;

namespace hundir_la_flota.Utils
{
    public static class DTOMapper
    {
        public static GameResponseDTO ToGameResponseDTO(Game game)
        {
            return new GameResponseDTO
            {
                GameId = game.GameId,
                Player1Nickname = game.Participants.FirstOrDefault(p => p.Role == ParticipantRole.Host)?.User.Nickname ?? "Vacante",
                Player2Nickname = game.Participants.FirstOrDefault(p => p.Role == ParticipantRole.Guest)?.User.Nickname ?? "Vacante",
                Player1Role = "Host",
                Player2Role = "Guest",
                StateDescription = game.State.ToString(),
                Player1Board = ToBoardDTO(game.Player1Board),
                Player2Board = ToBoardDTO(game.Player2Board),
                Actions = game.Actions.Select(ToGameActionDTO).ToList(),
                CurrentPlayerId = game.CurrentPlayerId ?? 0,
                CreatedAt = game.CreatedAt
            };
        }

        public static BoardDTO ToBoardDTO(Board board)
        {
            return new BoardDTO
            {
                Ships = board.Ships.Select(ToShipDTO).ToList(),
                Grid = board.Grid.Select(kvp => new CellDTO
                {
                    X = kvp.Value.X,
                    Y = kvp.Value.Y,
                    HasShip = kvp.Value.HasShip,
                    IsHit = kvp.Value.IsHit,
                    Status = kvp.Value.Status.ToString()
                }).ToList()
            };
        }


        private static ShipDTO ToShipDTO(Ship ship)
        {
            return new ShipDTO
            {
                Name = ship.Name,
                Size = ship.Size,
                Coordinates = ship.Coordinates.Select(ToCoordinateDTO).ToList()
            };
        }

        private static CoordinateDTO ToCoordinateDTO(Coordinate coord)
        {
            return new CoordinateDTO
            {
                X = coord.X,
                Y = coord.Y,
                IsHit = coord.IsHit
            };
        }

        public static GameActionDTO ToGameActionDTO(GameAction action)
        {
            return new GameActionDTO
            {
                ActionType = action.ActionType,
                Details = action.Details,
                Timestamp = action.Timestamp
            };
        }

    }
}


