using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;

namespace hundir_la_flota.Websocket
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var token = context.Request.Query["token"].ToString();
                var userId = ValidateTokenAndGetUserId(token);

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("Token inválido o usuario no identificado.");
                    context.Response.StatusCode = 401;
                    return;
                }

                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine($"Usuario {userId} conectado al WebSocket.");

                // Registrar el WebSocket en el diccionario de conexiones
                WebSocketController.ConnectedUsers[userId] = webSocket;

                // Llamar al método para manejar la conexión WebSocket
                await HandleWebSocketConnection(context, webSocket, userId);
            }
            else
            {
                await _next(context);
            }
        }

        private string ValidateTokenAndGetUserId(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Token vacío o nulo.");
                return null;
            }

            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("El token no contiene el 'nameid'.");
                    return null;
                }
                Console.WriteLine($"Token válido. UserID extraído: {userId}");
                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al validar el token: {ex.Message}");
                return null;
            }
        }

        private async Task HandleWebSocketConnection(HttpContext context, WebSocket webSocket, string userId)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                // Aquí puedes procesar mensajes recibidos
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Mensaje recibido de {userId}: {message}");

                // Procesar el mensaje recibido y enviarlo al cliente
                var response = Encoding.UTF8.GetBytes($"Echo: {message}");
                await webSocket.SendAsync(new ArraySegment<byte>(response), result.MessageType, result.EndOfMessage, CancellationToken.None);

            } while (!result.CloseStatus.HasValue);

            // Cuando el WebSocket se desconecte, eliminar el usuario del diccionario
            WebSocketController.ConnectedUsers.TryRemove(userId, out _);
            Console.WriteLine($"Usuario {userId} desconectado del WebSocket.");

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
