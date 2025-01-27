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
        Task NotifyUsersAsync(IEnumerable<int> userIds, string action, string payload);
        bool IsUserConnected(int userId);
        WebSocketService.UserState GetUserState(int userId);
        List<int> GetConnectedUserIds();
    }

    public class WebSocketService : IWebSocketService
    {
        public enum UserState { Disconnected, Connected, Playing }

        private readonly ConcurrentDictionary<int, WebSocket> _connectedUsers = new();
        private readonly ConcurrentDictionary<int, UserState> _userStates = new();
        private readonly ILogger<WebSocketService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WebSocketService(ILogger<WebSocketService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private IGameService GetGameService()
        {
            return _serviceProvider.GetRequiredService<IGameService>();
        }

        public List<int> GetConnectedUserIds()
        {
            return _connectedUsers.Keys.ToList();
        }

        public async Task HandleConnectionAsync(int userId, WebSocket webSocket)
        {
            var socketWrapper = new WebSocketWrapper(webSocket);

            if (!_connectedUsers.TryAdd(userId, webSocket))
            {
                _logger.LogWarning($"Usuario {userId} ya está conectado. Rechazando conexión.");
                await socketWrapper.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    "Usuario ya conectado"
                );
                return;
            }

            UpdateUserState(userId, UserState.Connected);
            await NotifyUserStatusChangeAsync(userId, UserState.Connected);

            var buffer = new byte[1024 * 4];
            try
            {
                while (socketWrapper.GetState() == WebSocketState.Open)
                {
                    var message = await socketWrapper.ReceiveMessageAsync(buffer);
                    if (string.IsNullOrEmpty(message)) break;

                    await ProcessMessageAsync(userId, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en la conexión WebSocket para el usuario {userId}: {ex.Message}");
            }
            finally
            {
                await DisconnectUserAsync(userId);
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

        public async Task NotifyUsersAsync(IEnumerable<int> userIds, string action, string payload)
        {
            foreach (var userId in userIds)
            {
                await NotifyUserAsync(userId, action, payload);
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
                    case "ChatMessage":
                        var payloadParts = payload.Split(':');
                        if (payloadParts.Length < 2)
                        {
                            _logger.LogWarning($"Formato de payload inválido para ChatMessage: {payload}");
                            return;
                        }

                        var gameId = Guid.Parse(payloadParts[0]);
                        var chatMessage = payloadParts[1];
                        await HandleChatMessageAsync(gameId, userId, chatMessage);
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

        private async Task HandleChatMessageAsync(Guid gameId, int senderId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                _logger.LogWarning($"Mensaje vacío recibido en el juego {gameId} de usuario {senderId}.");
                return;
            }

            var chatService = _serviceProvider.GetRequiredService<IChatService>();

            var response = await chatService.SendMessageAsync(gameId, senderId, message);
            if (!response.Success)
            {
                _logger.LogWarning($"Error al enviar mensaje: {response.Message}");
                return;
            }


            if (_connectedUsers.TryGetValue(senderId, out var senderWebSocket))
            {
                var notificationPayload = $"{senderId}:{message}";
                var gameUsers = _connectedUsers.Keys.Where(userId =>
                    userId != senderId);

                foreach (var userId in gameUsers)
                {
                    if (_connectedUsers.TryGetValue(userId, out var webSocket))
                    {
                        await SendMessageAsync(webSocket, "ChatMessage", notificationPayload);
                    }
                }
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
            UpdateUserState(userId, newState);

            var message = $"{userId}:{newState}";
            var tasks = _connectedUsers
                .Where(kvp => kvp.Key != userId)
                .Select(async kvp =>
                {
                    if (kvp.Value.State == WebSocketState.Open)
                    {
                        await SendMessageAsync(kvp.Value, "UserStatus", message);
                    }
                });

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogInformation($"Estado del usuario {userId} actualizado a {newState}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notificando cambios de estado para el usuario {userId}: {ex.Message}");
            }
        }


        public async Task SendMessageAsync(WebSocket webSocket, string action, string payload)
        {
            var socketWrapper = new WebSocketWrapper(webSocket);
            await socketWrapper.SendMessageAsync(action, payload);
        }


        public bool IsUserConnected(int userId)
        {
            return _connectedUsers.ContainsKey(userId);
        }

        public UserState GetUserState(int userId)
        {
            if (_userStates.TryGetValue(userId, out var state))
            {
                return state;
            }
            return UserState.Disconnected;
        }

        private void UpdateUserState(int userId, UserState newState)
        {
            _userStates[userId] = newState;
        }
    }
}
