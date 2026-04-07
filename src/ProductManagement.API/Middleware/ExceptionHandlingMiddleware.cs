using System.Diagnostics;
using System.Text.Json;
using ProductManagement.Application.Common.Exceptions;

namespace ProductManagement.API.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException e    => (StatusCodes.Status404NotFound,            "Not Found",               e.Message),
            ConflictException e    => (StatusCodes.Status409Conflict,            "Conflict",                e.Message),
            ConcurrencyException e => (StatusCodes.Status409Conflict,            "Conflict",                e.Message),
            BusinessException e    => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity",    e.Message),
            _                      => (StatusCodes.Status500InternalServerError, "Internal Server Error",   "An unexpected error occurred.")
        };

        if (exception is NotFoundException or ConflictException or ConcurrencyException or BusinessException)
            logger.LogWarning(exception, "Handled exception {ExceptionType}: {Message}",
                exception.GetType().Name, exception.Message);
        else
            logger.LogError(exception, "Unhandled exception");

        var problemDetails = new
        {
            type     = "https://tools.ietf.org/html/rfc7807",
            title,
            status   = statusCode,
            detail,
            instance = context.Request.Path.Value,
            traceId  = Activity.Current?.Id ?? context.TraceIdentifier
        };

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, JsonOptions));
    }
}
