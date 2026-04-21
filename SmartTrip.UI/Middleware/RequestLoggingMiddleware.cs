using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SmartTrip.UI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;
            request.EnableBuffering();

            string body = string.Empty;
            if (request.ContentLength is > 0)
            {
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            var headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.LogInformation(
                "Request info: Method={Method}, Url={Url}, Ip={Ip}, UserId={UserId}, Headers={Headers}, Body={Body}",
                request.Method,
                $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}",
                context.Connection.RemoteIpAddress?.ToString(),
                userId ?? "anonymous",
                JsonSerializer.Serialize(headers),
                body);

            await _next(context);
        }
    }
}
