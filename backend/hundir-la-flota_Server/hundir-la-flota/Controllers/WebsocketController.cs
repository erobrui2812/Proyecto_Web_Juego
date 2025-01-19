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
            return BadRequest("La solicitud no es un WebSocket");

        var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            return Unauthorized("Token no proporcionado o inválido.");

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var userId = _authService.GetUserIdFromToken(token);
            if (!userId.HasValue)
                return Unauthorized("Token inválido o usuario no autorizado.");

            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _webSocketService.HandleConnectionAsync(userId.Value, webSocket);

            return Ok("Conexión WebSocket establecida.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en la conexión WebSocket: {ex.Message}");
            return StatusCode(500, "Error en el servidor.");
        }
    }
}
