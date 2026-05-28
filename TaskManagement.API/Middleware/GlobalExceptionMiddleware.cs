using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Models.Errors;

namespace TaskManagement.API.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => HttpStatusCode.Forbidden,
            KeyNotFoundException => HttpStatusCode.NotFound,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            DbUpdateException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        // Log full details server-side; never return stack traces to the client.
        _logger.LogError(
            exception,
            "Unhandled exception. TraceId: {TraceId}, StatusCode: {StatusCode}, Inner: {InnerMessage}",
            context.TraceIdentifier,
            (int)statusCode,
            GetInnermostMessage(exception));

        var clientMessage = statusCode == HttpStatusCode.InternalServerError && !_environment.IsDevelopment()
            ? "An unexpected error occurred."
            : GetClientSafeMessage(exception, _environment.IsDevelopment());

        var response = new ApiErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = clientMessage,
            TraceId = context.TraceIdentifier
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static string GetClientSafeMessage(Exception exception, bool isDevelopment)
    {
        if (exception is DbUpdateException dbUpdate)
        {
            var inner = GetInnermostMessage(dbUpdate);
            if (inner.Contains("Invalid column name", StringComparison.OrdinalIgnoreCase))
            {
                return "Database schema is out of date. Apply pending EF Core migrations and try again.";
            }

            return isDevelopment
                ? $"Unable to save changes: {inner}"
                : "Unable to save task. Verify required fields and database migrations.";
        }

        if (exception is InvalidOperationException invalidOp)
        {
            // TaskService wraps DbUpdateException with the real SQL message in InvalidOperationException.Message.
            return invalidOp.Message;
        }

        return isDevelopment ? exception.Message : "An unexpected error occurred.";
    }

    private static string GetInnermostMessage(Exception exception)
    {
        while (exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        return exception.Message;
    }
}
