using ProductManagement.API.Middleware;

namespace ProductManagement.API.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionHandlingMiddleware>();

    public static IApplicationBuilder UseValidationExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<ValidationExceptionMiddleware>();
}
