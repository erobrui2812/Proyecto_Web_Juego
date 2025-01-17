using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace hundir_la_flota.Services
{
    public class WebSocketService
    {
        public enum UserState {Disconnected,Connected,Playing}

        public readonly ConcurrentDictionary<string, WebSocket> _connectedUsers = new();
        public readonly ConcurrentDictionary<string, UserState> _userStates = new();

        public async Task HandleConnectionAsync(string userId, WebSocket webSocket)
        {
            if (!_connectedUsers.TryAdd(userId, webSocket))
            {
                Console.WriteLine($"El usuario {userId} ya está conectado.");
                await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Usuario ya conectado", CancellationToken.None);
                return;
            }

            _userStates[userId] = UserState.Connected;
            await NotifyUserStatusChangeAsync(userId, UserState.Connected);

            Console.WriteLine($"Usuario {userId} conectado.");

            var buffer = new byte[1024 * 4];
            try
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Mensaje recibido de {userId}: {message}");
                    await ProcessMessageAsync(userId, message);
                } while (!result.CloseStatus.HasValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en la conexión WebSocket de {userId}: {ex.Message}");
            }
            finally
            {
                await DisconnectUserAsync(userId);
            }
        }

        private async Task ProcessMessageAsync(string userId, string message)
        {
            var parts = message.Split('|');
            if (parts.Length < 2)
            {
                Console.WriteLine($"Formato de mensaje inválido de {userId}.");
                return;
            }

            var action = parts[0];
            var payload = parts[1];

            switch (action)
            {
                case "StartGame":
                    Console.WriteLine($"El usuario {userId} ha iniciado un juego.");
                    _userStates[userId] = UserState.Playing;
                    await NotifyUserStatusChangeAsync(userId, UserState.Playing);
                    break;

                case "EndGame":
                    Console.WriteLine($"El usuario {userId} ha terminado el juego.");
                    _userStates[userId] = UserState.Connected;
                    await NotifyUserStatusChangeAsync(userId, UserState.Connected);
                    break;

                case "FriendRequest":
                    Console.WriteLine($"Solicitud de amistad de {userId} a {payload}");
                    await HandleSendFriendRequestAsync(userId, payload);
                    break;

                default:
                    Console.WriteLine($"Acción no reconocida: {action}");
                    if (_connectedUsers.TryGetValue(userId, out var userWebSocket))
                    {
                        await SendMessageAsync(userWebSocket, "UnknownAction", "Acción no reconocida.");
                    }
                    break;
            }
        }

        private async Task HandleSendFriendRequestAsync(string senderId, string recipientId)
        {
            if (_connectedUsers.TryGetValue(recipientId, out var recipientWebSocket))
            {
                Console.WriteLine($"Enviando solicitud de amistad de {senderId} a {recipientId}");
                await SendMessageAsync(recipientWebSocket, "FriendRequest", senderId);
            }
            else
            {
                Console.WriteLine($"El usuario {recipientId} no está conectado.");
                await SendMessageAsync(recipientWebSocket, "FriendRequest", senderId);
            }
        }

        private async Task NotifyUserStatusChangeAsync(string userId, UserState newState)
        {
            var message = $"{userId}|{newState}";
            foreach (var connection in _connectedUsers.Values)
            {
                await SendMessageAsync(connection, "UserStatus", message);
            }
        }

        private async Task SendMessageAsync(WebSocket webSocket, string action, string payload)
        {
            var message = $"{action}|{payload}";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task DisconnectUserAsync(string userId)
        {
            if (_connectedUsers.TryRemove(userId, out var webSocket))
            {
                _userStates[userId] = UserState.Disconnected;
                await NotifyUserStatusChangeAsync(userId, UserState.Disconnected);

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconexión", CancellationToken.None);
                }
                webSocket.Dispose();
                Console.WriteLine($"Usuario {userId} desconectado.");
            }
        }
    }
}
