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
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocketConnection(context, webSocket);
            }
            else
            {
                await _next(context);
            }
        }

        private async Task HandleWebSocketConnection(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                // Aquí puedes procesar mensajes recibidos
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Mensaje recibido: {message}");

                // Ejemplo de respuesta al cliente
                var response = Encoding.UTF8.GetBytes($"Echo: {message}");
                await webSocket.SendAsync(new ArraySegment<byte>(response), result.MessageType, result.EndOfMessage, CancellationToken.None);

            } while (!result.CloseStatus.HasValue);

            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
