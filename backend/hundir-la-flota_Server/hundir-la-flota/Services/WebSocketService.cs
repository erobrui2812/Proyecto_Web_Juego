using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace hundir_la_flota.Services
{
    public interface IWebSocketService
    {
        Task HandleConnectionAsync(int userId, WebSocket webSocket);
        Task NotifyUserStatusChangeAsync(int userId, WebSocketService.UserState newState);
        Task SendMessageAsync(WebSocket webSocket, string action, string payload);
        Task DisconnectUserAsync(int userId);
        Task NotifyUserAsync(int userId, string action, string payload);
        bool IsUserConnected(int userId);
    }

    public class WebSocketService : IWebSocketService
    {
        public enum UserState { Disconnected, Connected, Playing }

        private readonly ConcurrentDictionary<int, WebSocket> _connectedUsers = new();
        private readonly ConcurrentDictionary<int, UserState> _userStates = new();
        private readonly ILogger<WebSocketService> _logger;

        public WebSocketService(ILogger<WebSocketService> logger)
        {
            _logger = logger;
        }

        public async Task HandleConnectionAsync(int userId, WebSocket webSocket)
        {
            if (!_connectedUsers.TryAdd(userId, webSocket))
            {
                _logger.LogWarning($"Usuario {userId} ya está conectado. Rechazando conexión.");
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    "Usuario ya conectado",
                    CancellationToken.None
                );
                return;
            }

            UpdateUserState(userId, UserState.Connected);
            _logger.LogInformation($"Usuario {userId} conectado.");

            var buffer = new byte[1024 * 4];
            try
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogInformation($"Mensaje recibido de {userId}: {message}");

                    await ProcessMessageAsync(userId, message);
                }
                while (!result.CloseStatus.HasValue);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en la conexión WebSocket de {userId}: {ex.Message}");
            }
            finally
            {
                await DisconnectUserAsync(userId);
            }
        }

        public async Task NotifyUserAsync(int userId, string action, string payload)
        {
            if (_connectedUsers.TryGetValue(userId, out var webSocket))
            {
                await SendMessageAsync(webSocket, action, payload);
            }
            else
            {
                _logger.LogWarning($"Intento de notificar al usuario {userId}, pero no está conectado.");
            }
        }

        private async Task ProcessMessageAsync(int userId, string message)
        {
            try
            {
                var parts = message.Split('|');
                if (parts.Length < 2)
                {
                    _logger.LogWarning($"Formato de mensaje inválido de {userId}: {message}");
                    return;
                }

                var action = parts[0];
                var payload = parts[1];

                switch (action)
                {
                    case "StartGame":
                        UpdateUserState(userId, UserState.Playing);
                        await NotifyUserStatusChangeAsync(userId, UserState.Playing);
                        _logger.LogInformation($"Usuario {userId} ha iniciado un juego.");
                        break;

                    case "EndGame":
                        UpdateUserState(userId, UserState.Connected);
                        await NotifyUserStatusChangeAsync(userId, UserState.Connected);
                        _logger.LogInformation($"Usuario {userId} ha terminado un juego.");
                        break;

                    case "FriendRequest":
                        if (int.TryParse(payload, out var recipientId))
                        {
                            await HandleSendFriendRequestAsync(userId, recipientId);
                        }
                        else
                        {
                            _logger.LogWarning($"Payload inválido para FriendRequest: {payload}");
                        }
                        break;

                    case "AbandonGame":
                        await NotifyUserStatusChangeAsync(userId, UserState.Connected);
                        _logger.LogInformation($"Usuario {userId} ha abandonado un juego.");
                        break;

                    default:
                        _logger.LogWarning($"Acción no reconocida: {action}");
                        if (_connectedUsers.TryGetValue(userId, out var webSocket))
                        {
                            await SendMessageAsync(webSocket, "UnknownAction", "Acción no reconocida.");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error procesando mensaje de {userId}: {ex.Message}");
            }
        }

        private async Task HandleSendFriendRequestAsync(int senderId, int recipientId)
        {
            if (_connectedUsers.TryGetValue(recipientId, out var recipientWebSocket))
            {
                _logger.LogInformation($"Enviando solicitud de amistad de {senderId} a {recipientId}");
                await SendMessageAsync(recipientWebSocket, "FriendRequest", senderId.ToString());
            }
            else
            {
                _logger.LogWarning($"El usuario {recipientId} no está conectado. No se pudo enviar la solicitud de amistad.");
            }
        }

        public async Task NotifyUserStatusChangeAsync(int userId, UserState newState)
        {
            var message = $"{userId}|{newState}";
            foreach (var kvp in _connectedUsers)
            {
                await SendMessageAsync(kvp.Value, "UserStatus", message);
            }
        }

        public async Task SendMessageAsync(WebSocket webSocket, string action, string payload)
        {
            try
            {
                var message = $"{action}|{payload}";
                var messageBytes = Encoding.UTF8.GetBytes(message);

                await webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enviando mensaje por WebSocket: {ex.Message}");
            }
        }

        public async Task DisconnectUserAsync(int userId)
        {
            if (_connectedUsers.TryRemove(userId, out var webSocket))
            {
                UpdateUserState(userId, UserState.Disconnected);
                await NotifyUserStatusChangeAsync(userId, UserState.Disconnected);

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Desconexión", CancellationToken.None);
                }

                webSocket.Dispose();
                _logger.LogInformation($"Usuario {userId} desconectado.");
            }


        }
        public bool IsUserConnected(int userId)
        {
            return _connectedUsers.ContainsKey(userId);
        }

        private void UpdateUserState(int userId, UserState newState)
        {
            _userStates[userId] = newState;
        }
    }
}
