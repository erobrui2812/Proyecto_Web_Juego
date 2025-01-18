using hundir_la_flota.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly WebSocketService _webSocketService;
    private readonly AuthService _authService;


    public WebSocketController(WebSocketService webSocketService, AuthService authService)
    {
        _webSocketService = webSocketService;
        _authService = authService;
    }

    [HttpGet]
    public async Task Connect()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {

            var authorizationHeader = HttpContext.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Bearer "))
            {
                Console.WriteLine("Conexión WebSocket rechazada: token no proporcionado.");
                HttpContext.Response.StatusCode = 401;
                return;
            }

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();
            Console.WriteLine($"Verificando token: {token}");

            try
            {

                var userId = _authService.GetUserIdFromTokenAsInt("Bearer " + token);
                if (!userId.HasValue)
                {
                    Console.WriteLine("Conexión WebSocket rechazada: token inválido o no se pudo extraer userId.");
                    HttpContext.Response.StatusCode = 401;
                    return;
                }

                Console.WriteLine($"Token recibido: {token}");
                var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();


                await _webSocketService.HandleConnectionAsync(userId.Value, webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al manejar la conexión WebSocket: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Conexión WebSocket rechazada: no es una solicitud WebSocket.");
            HttpContext.Response.StatusCode = 400;
        }
    }
}
