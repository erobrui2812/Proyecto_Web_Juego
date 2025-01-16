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
                HttpContext.Response.StatusCode = 401;
                return;
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

            ConnectedUsers[userId] = webSocket;
            UserStatuses[userId] = "Conectado";
            await SendUserStatusUpdate(userId, "Conectado");

            await HandleWebSocketConnection(userId, webSocket);

            ConnectedUsers.TryRemove(userId, out _);
            UserStatuses[userId] = "Desconectado";
            await SendUserStatusUpdate(userId, "Desconectado");
        }
        else
        {
            HttpContext.Response.StatusCode = 400; // Petición incorrecta
        }
    }

    private string ValidateTokenAndGetUserId(string token)
    {
        Console.WriteLine($"Validando token: {token}");
        if (string.IsNullOrEmpty(token)) return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
            Console.WriteLine($"Token válido. UserID extraído: {userId}");
            return userId;
        }
        catch
        {
            Console.WriteLine($"Error al validar el token");
            return null;
        }
    }


    private async Task HandleWebSocketConnection(string userId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
            else
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await ProcessMessage(userId, message, webSocket);
            }
        }
    }

    private async Task ProcessMessage(string userId, string message, WebSocket webSocket)
    {
        var parts = message.Split('|');
        if (parts.Length < 2) return;

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
                await SendMessage(webSocket, "UnknownAction", "Acción no reconocida.");
                break;
        }
    }

    private async Task HandleSendFriendRequest(string senderId, string recipientId)
    {
        if (ConnectedUsers.TryGetValue(recipientId, out var recipientWebSocket))
        {
            await SendMessage(recipientWebSocket, "FriendRequest", senderId);
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
        }
    }

    private async Task HandleInviteFriend(string inviterId, string friendId)
    {
        if (ConnectedUsers.TryGetValue(friendId, out var friendWebSocket))
        {
            await SendMessage(friendWebSocket, "GameInvitation", inviterId);
        }
    }

    private async Task HandleUpdateStatus(string userId, string status)
    {
        UserStatuses[userId] = status;
        await SendUserStatusUpdate(userId, status);
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

        if (WebSocketController.ConnectedUsers.TryGetValue(userId, out var webSocket))
        {
            var message = $"{action}|{payload}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            return Ok($"Mensaje de prueba enviado a {userId}: {message}");
        }

        return NotFound($"El usuario {userId} no está conectado.");
    }

}
