using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using hundir_la_flota.Services;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly WebSocketService _webSocketService;

    public WebSocketController(WebSocketService webSocketService)
    {
        _webSocketService = webSocketService;
    }

    [HttpGet]
    public async Task Connect()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            string token = null;

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                token = authorizationHeader.Substring("Bearer ".Length).Trim();
            }

            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Conexión WebSocket rechazada: token no proporcionado.");
                HttpContext.Response.StatusCode = 401;
                return;
            }

            Console.WriteLine($"Token recibido: {token}");
            Console.WriteLine("Conexión WebSocket establecida.");

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _webSocketService.HandleConnectionAsync(token, webSocket);
        }
        else
        {
            Console.WriteLine("Conexión WebSocket rechazada: no es una solicitud WebSocket.");
            HttpContext.Response.StatusCode = 400;
        }
    }

    [HttpPost("test-message")]
    public async Task<IActionResult> SendTestMessage([FromQuery] string userId, [FromQuery] string action, [FromQuery] string payload)
    {
        if (string.IsNullOrEmpty(userId))
            return BadRequest("Debe proporcionar un userId.");

        if (_webSocketService._connectedUsers.TryGetValue(userId, out var webSocket))
        {
            var message = $"{action}|{payload}";
            var messageBytes = Encoding.UTF8.GetBytes(message);

            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            return Ok($"Mensaje de prueba enviado a {userId}: {message}");
        }

        return NotFound($"El usuario {userId} no está conectado.");
    }
}
