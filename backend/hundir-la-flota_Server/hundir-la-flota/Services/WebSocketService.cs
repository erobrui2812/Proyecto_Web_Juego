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

        // esto es la cola
        private static readonly ConcurrentQueue<int> _matchmakingQueue = new();

        private readonly ConcurrentDictionary<int, WebSocket> _connectedUsers = new();
        private readonly ConcurrentDictionary<int, UserState> _userStates = new();
        private readonly ILogger<WebSocketService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public WebSocketService(ILogger<WebSocketService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
                _logger.LogWarning($"User {userId} is already connected. Connection rejected.");
                await socketWrapper.CloseAsync(
                    WebSocketCloseStatus.PolicyViolation,
                    "User already connected"
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
                _logger.LogError($"WebSocket error for user {userId}: {ex.Message}");
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


                RemoveFromMatchmakingQueue(userId);

                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
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
                _logger.LogWarning($"Attempt to notify user {userId}, but they are not connected.");
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
                    _logger.LogWarning($"Invalid message format from {userId}: {message}");
                    return;
                }

                var action = parts[0];
                var payload = parts[1];

                switch (action)
                {
                    case "ChatMessage":
                        await HandleChatMessageAsync(userId, payload);
                        break;

                    case "Matchmaking":
                        // Ejemplo: "Matchmaking|random" o "Matchmaking|cancel"
                        await HandleMatchmakingAsync(userId, payload);
                        break;

                    default:
                        _logger.LogWarning($"Unrecognized action: {action}");
                        if (_connectedUsers.TryGetValue(userId, out var webSocket))
                        {
                            await SendMessageAsync(webSocket, "UnknownAction", "Unrecognized action.");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message from {userId}: {ex.Message}");
            }
        }

        private async Task HandleChatMessageAsync(int senderId, string payload)
        {

            var payloadParts = payload.Split(':');
            if (payloadParts.Length < 2)
            {
                _logger.LogWarning($"Invalid payload for ChatMessage: {payload}");
                return;
            }

            var gameId = Guid.Parse(payloadParts[0]);
            var chatMessage = payloadParts[1];

            if (string.IsNullOrWhiteSpace(chatMessage))
            {
                _logger.LogWarning($"Empty chat message received in game {gameId} from user {senderId}.");
                return;
            }

            var chatService = _serviceProvider.GetRequiredService<IChatService>();
            var response = await chatService.SendMessageAsync(gameId, senderId, chatMessage);

            if (!response.Success)
            {
                _logger.LogWarning($"Error sending chat message: {response.Message}");
                return;
            }


            var notificationPayload = $"{senderId}:{chatMessage}";
            var recipients = _connectedUsers.Keys.Where(uid => uid != senderId);

            foreach (var userId in recipients)
            {
                if (_connectedUsers.TryGetValue(userId, out var webSocket))
                {
                    await SendMessageAsync(webSocket, "ChatMessage", notificationPayload);
                }
            }
        }

        private async Task HandleMatchmakingAsync(int userId, string payload)
        {
            if (payload == "random")
            {
              
                if (!_matchmakingQueue.Contains(userId))
                {
                    _matchmakingQueue.Enqueue(userId);
                    _logger.LogInformation($"User {userId} added to matchmaking queue.");
                }

          
                await MatchUsersInQueue();
            }
            else if (payload == "cancel")
            {
              
                RemoveFromMatchmakingQueue(userId);
            }
        }

        private async Task MatchUsersInQueue()
        {
          
            while (_matchmakingQueue.Count >= 2)
            {
                if (_matchmakingQueue.TryDequeue(out int user1) &&
                    _matchmakingQueue.TryDequeue(out int user2))
                {
                    _logger.LogInformation($"Matchmaking pair found: {user1} and {user2}");

                 
                    using var scope = _serviceProvider.CreateScope();
                    var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

                 
                    var createGameResponse = await gameService.CreateGameAsync(user1.ToString());
                    if (!createGameResponse.Success)
                    {
                        _logger.LogError($"Error creating game: {createGameResponse.Message}");
                     
                        continue;
                    }

                    var newGame = createGameResponse.Data;

                
                    var joinResponse = await gameService.JoinGameAsync(newGame.GameId, user2);
                    if (!joinResponse.Success)
                    {
                        _logger.LogError($"Error joining user {user2} to game {newGame.GameId}: {joinResponse.Message}");
                       
                        continue;
                    }

                  
                    await NotifyUserAsync(user1, "MatchFound", newGame.GameId.ToString());
                    await NotifyUserAsync(user2, "MatchFound", newGame.GameId.ToString());
                }
            }
        }

        private void RemoveFromMatchmakingQueue(int userId)
        {

            var allUsers = _matchmakingQueue.ToList();
            var newQueue = new ConcurrentQueue<int>(allUsers.Where(u => u != userId));


            while (_matchmakingQueue.TryDequeue(out _))
                foreach (var u in newQueue) _matchmakingQueue.Enqueue(u);

            _logger.LogInformation($"User {userId} removed from matchmaking queue (if was present).");
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
                _logger.LogInformation($"User {userId} state changed to {newState}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error notifying user status change for {userId}: {ex.Message}");
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
