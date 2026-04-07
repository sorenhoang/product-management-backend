using System.Text.Json;
using FluentValidation;

namespace ProductManagement.API.Middleware;

public class ValidationExceptionMiddleware(
    RequestDelegate next,
    ILogger<ValidationExceptionMiddleware> logger)
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
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
    }

    private async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        logger.LogWarning("Validation failed with {ErrorCount} error(s): {Errors}",
            exception.Errors.Count(),
            string.Join("; ", exception.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

        var response = new
        {
            type    = "https://tools.ietf.org/html/rfc7807",
            title   = "Validation failed.",
            status  = StatusCodes.Status400BadRequest,
            detail  = "One or more validation errors occurred.",
            errors
        };

        context.Response.StatusCode  = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        // Handle paths like "InitialVariants[0].Sku" → "initialVariants[0].sku"
        var segments = propertyName.Split('.');
        var converted = segments.Select(segment =>
        {
            if (string.IsNullOrEmpty(segment)) return segment;
            var bracketIndex = segment.IndexOf('[');
            if (bracketIndex > 0)
            {
                var name = segment[..bracketIndex];
                var rest = segment[bracketIndex..];
                return char.ToLowerInvariant(name[0]) + name[1..] + rest;
            }
            return char.ToLowerInvariant(segment[0]) + segment[1..];
        });

        return string.Join('.', converted);
    }
}
