using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly IWebSocketService _webSocketService;
    private readonly IAuthService _authService;

    public WebSocketController(IWebSocketService webSocketService, IAuthService authService)
    {
        _webSocketService = webSocketService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> Connect()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            Console.WriteLine("Solicitud rechazada: No es una solicitud de WebSocket.");
            return BadRequest("La solicitud no es un WebSocket");
        }

        var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            Console.WriteLine("Solicitud rechazada: Token de autorización no proporcionado o inválido.");
            return Unauthorized("Token no proporcionado o inválido.");
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var userId = _authService.GetUserIdFromToken(token);
            if (!userId.HasValue)
            {
                Console.WriteLine("Solicitud rechazada: Token inválido o usuario no autorizado.");
                return Unauthorized("Token inválido o usuario no autorizado.");
            }

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _webSocketService.HandleConnectionAsync(userId.Value, webSocket);

            Console.WriteLine($"Usuario {userId.Value} conectado vía WebSocket.");
            return Ok("Conexión WebSocket establecida.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en la conexión WebSocket: {ex.Message}");
            return StatusCode(500, "Error en el servidor.");
        }
    }
}
