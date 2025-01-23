using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("ws")]
public class WebSocketController
{
    private readonly IWebSocketService _webSocketService;
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        IWebSocketService webSocketService,
        IAuthService authService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<WebSocketController> logger)
    {
        _webSocketService = webSocketService;
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Connect()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            _logger.LogError("No se pudo obtener el contexto HTTP.");
            return new ObjectResult("Error en el servidor.") { StatusCode = 500 };
        }

        if (!httpContext.WebSockets.IsWebSocketRequest)
        {
            _logger.LogWarning("Solicitud rechazada: No es una solicitud de WebSocket.");
            return new BadRequestObjectResult("La solicitud no es un WebSocket.");
        }

        var authorizationHeader = httpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Solicitud rechazada: Token de autorización no proporcionado o inválido.");
            return new UnauthorizedObjectResult("Token no proporcionado o inválido.");
        }

        var token = authorizationHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var userId = _authService.GetUserIdFromToken(token);
            if (!userId.HasValue)
            {
                _logger.LogWarning("Solicitud rechazada: Token inválido o usuario no autorizado.");
                return new UnauthorizedObjectResult("Token inválido o usuario no autorizado.");
            }

            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
            _logger.LogInformation($"Usuario {userId.Value} conectado vía WebSocket.");

           
            await _webSocketService.HandleConnectionAsync(userId.Value, webSocket);

            
            return new EmptyResult(); 
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error en la conexión WebSocket: {ex.Message}");
            return new ObjectResult("Error en el servidor.") { StatusCode = 500 };
        }
    }

}