public class WebSocketMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WebSocketMiddleware> _logger;

    public WebSocketMiddleware(RequestDelegate next, ILogger<WebSocketMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var token = context.Request.Query["token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Token faltante en solicitud WebSocket.");
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 400;
                }
                return;
            }

            context.Request.Method = "GET";

            _logger.LogInformation($"Token extraído del query: {token}");
            context.Request.Headers["Authorization"] = $"Bearer {token}";
        }

        await _next(context);
    }
}
