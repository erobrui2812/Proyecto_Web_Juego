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
        Console.WriteLine($"Validando token: {token}");
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

        while (webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine($"Conexión cerrada por el usuario: {userId}");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Conexión cerrada por el cliente", CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Mensaje recibido de {userId}: {message}");
                    await ProcessMessage(userId, message, webSocket);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en WebSocket para el usuario {userId}: {ex.Message}");
                break;
            }
        }

        Console.WriteLine($"Conexión finalizada para el usuario: {userId}");
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
            case "SendFriendRequest":
                await HandleSendFriendRequest(userId, payload);
                break;

            case "RespondFriendRequest":
                await HandleRespondFriendRequest(userId, payload);
                break;

            case "InviteFriend":
                await HandleInviteFriend(userId, payload);
                break;

            case "UpdateStatus":
                await HandleUpdateStatus(userId, payload);
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
            await SendMessage(recipientWebSocket, "FriendRequest", senderId);
            Console.WriteLine($"Solicitud de amistad enviada de {senderId} a {recipientId}");
        }
        else
        {
            Console.WriteLine($"El usuario {recipientId} no está conectado.");
        }
    }

    private async Task HandleRespondFriendRequest(string recipientId, string responsePayload)
    {
        var parts = responsePayload.Split(',');
        if (parts.Length < 2) return;

        var senderId = parts[0];
        var accepted = parts[1] == "true";

        if (ConnectedUsers.TryGetValue(senderId, out var senderWebSocket))
        {
            await SendMessage(senderWebSocket, "FriendRequestResponse", accepted ? "Accepted" : "Rejected");
            Console.WriteLine($"Respuesta de amistad de {recipientId} a {senderId}: {(accepted ? "Aceptada" : "Rechazada")}");
        }
    }

    private async Task HandleInviteFriend(string inviterId, string friendId)
    {
        if (ConnectedUsers.TryGetValue(friendId, out var friendWebSocket))
        {
            await SendMessage(friendWebSocket, "GameInvitation", inviterId);
            Console.WriteLine($"Invitación de juego enviada de {inviterId} a {friendId}");
        }
    }

    private async Task HandleUpdateStatus(string userId, string status)
    {
        UserStatuses[userId] = status;
        await SendUserStatusUpdate(userId, status);
        Console.WriteLine($"Estado actualizado para el usuario {userId}: {status}");
    }

    private async Task SendUserStatusUpdate(string userId, string status)
    {
        var message = $"UserStatus|{userId}|{status}";

        foreach (var connection in ConnectedUsers.Values)
        {
            await SendMessage(connection, "UserStatus", message);
        }
    }

    private async Task SendMessage(WebSocket webSocket, string action, string payload)
    {
        var message = $"{action}|{payload}";
        var messageBytes = Encoding.UTF8.GetBytes(message);
        await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
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
