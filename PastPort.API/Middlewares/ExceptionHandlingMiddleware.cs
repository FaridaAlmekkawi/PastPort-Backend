using System.Net;
using System.Text.Json;

namespace PastPort.API.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An error occurred while processing your request";

        if (exception is UnauthorizedAccessException)
        {
            statusCode = HttpStatusCode.Unauthorized;
            message = "Unauthorized access";
        }
        else if (exception is ArgumentException or InvalidOperationException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = exception.Message;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = message,
            statusCode = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}