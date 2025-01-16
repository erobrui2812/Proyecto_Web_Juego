using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    public static readonly ConcurrentDictionary<string, WebSocket> ConnectedUsers = new();
    private static readonly ConcurrentDictionary<string, string> UserStatuses = new();

    [HttpGet("connect")]
    public async Task Connect()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var token = HttpContext.Request.Query["token"].ToString();
            var userId = ValidateTokenAndGetUserId(token);

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("Token inválido o usuario no identificado.");
                HttpContext.Response.StatusCode = 401;
                return;
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            ConnectedUsers[userId] = webSocket;
            UserStatuses[userId] = "Conectado";
            await SendUserStatusUpdate(userId, "Conectado");

            Console.WriteLine($"Usuario conectado: {userId}");

            await HandleWebSocketConnection(userId, webSocket);

            ConnectedUsers.TryRemove(userId, out _);
            UserStatuses[userId] = "Desconectado";
            await SendUserStatusUpdate(userId, "Desconectado");
        }
        else
        {
            Console.WriteLine("Conexión WebSocket rechazada: no es una solicitud WebSocket.");
            HttpContext.Response.StatusCode = 400;
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

    private async Task HandleWebSocketConnection(string userId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result;

        do
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Mensaje recibido de {userId}: {message}");

            await ProcessMessage(userId, message, webSocket);
        } while (!result.CloseStatus.HasValue);

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }

    private async Task ProcessMessage(string userId, string message, WebSocket webSocket)
    {
        var parts = message.Split('|');
        if (parts.Length < 2)
        {
            Console.WriteLine("Formato de mensaje inválido.");
            return;
        }

        var action = parts[0];
        var payload = parts[1];

        switch (action)
        {
            case "FriendRequest":
                Console.WriteLine($"Recibida solicitud de amistad de {userId} a {payload}");
                await HandleSendFriendRequest(userId, payload);
                break;

            default:
                Console.WriteLine($"Acción no reconocida: {action}");
                await SendMessage(webSocket, "UnknownAction", "Acción no reconocida.");
                break;
        }
    }

    private async Task HandleSendFriendRequest(string senderId, string recipientId)
    {
        if (ConnectedUsers.TryGetValue(recipientId, out var recipientWebSocket))
        {
            Console.WriteLine($"Enviando solicitud de amistad a {recipientId} desde {senderId}");
            await SendMessage(recipientWebSocket, "FriendRequest", senderId);
        }
        else
        {
            Console.WriteLine($"El usuario {recipientId} no está conectado.");
        }
    }

    private async Task SendMessage(WebSocket webSocket, string action, string payload)
    {
        var message = $"{action}|{payload}";
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task SendUserStatusUpdate(string userId, string status)
    {
        var message = $"UserStatus|{userId}|{status}";

        foreach (var connection in ConnectedUsers.Values)
        {
            await SendMessage(connection, "UserStatus", message);
        }
    }


    [HttpPost("test-message")]
    public async Task<IActionResult> SendTestMessage([FromQuery] string userId, [FromQuery] string action, [FromQuery] string payload)
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest("Debe proporcionar un userId.");

        if (ConnectedUsers.TryGetValue(userId, out var webSocket))
        {
            var message = $"{action}|{payload}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            return Ok($"Mensaje de prueba enviado a {userId}: {message}");
        }

        return NotFound($"El usuario {userId} no está conectado.");
    }
}
