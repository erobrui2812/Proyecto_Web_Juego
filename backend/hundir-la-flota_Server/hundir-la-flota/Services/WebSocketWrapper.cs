using System.Net.WebSockets;
using System.Text;

namespace hundir_la_flota.Services
{
    public class WebSocketWrapper
    {
        private readonly WebSocket _webSocket;

        public WebSocketWrapper(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public async Task SendMessageAsync(string action, string payload)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("El WebSocket está cerrado y no puede enviar mensajes.");
            }

            try
            {
                var message = $"{action}|{payload}";
                var messageBytes = Encoding.UTF8.GetBytes(message);

                await _webSocket.SendAsync(
                    new ArraySegment<byte>(messageBytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enviando mensaje por WebSocket: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ReceiveMessageAsync(byte[] buffer)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                return Encoding.UTF8.GetString(buffer, 0, result.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recibiendo mensaje por WebSocket: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(closeStatus, statusDescription, CancellationToken.None);
            }
        }

        public WebSocketState GetState() => _webSocket.State;
    }
}
