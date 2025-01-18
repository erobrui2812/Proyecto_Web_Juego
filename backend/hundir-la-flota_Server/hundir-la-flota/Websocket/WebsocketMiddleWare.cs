using System.IdentityModel.Tokens.Jwt;
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
                var token = context.Request.Query["token"].ToString();

                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("Token faltante en la solicitud WebSocket.");
                    context.Response.StatusCode = 400;
                    return;
                }

                Console.WriteLine($"Token extraído del query: {token}");
                context.Request.Headers["Authorization"] = $"Bearer {token}";
            }

            await _next(context);
        }

    }
}