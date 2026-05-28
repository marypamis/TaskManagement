namespace TaskManagement.API.Middleware;

public class UnauthorizedLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UnauthorizedLoggingMiddleware> _logger;

    public UnauthorizedLoggingMiddleware(RequestDelegate next, ILogger<UnauthorizedLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            _logger.LogWarning(
                "Unauthorized access attempt. StatusCode: {StatusCode}, Method: {Method}, Path: {Path}, User: {User}",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                context.User.Identity?.Name ?? "anonymous");
        }
    }
}
