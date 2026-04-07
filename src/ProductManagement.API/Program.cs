using ProductManagement.API.Extensions;
using ProductManagement.Infrastructure.Extensions;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    // [SERVICES: Controllers]
    builder.Services.AddControllers();

    // [SERVICES: Scalar/Swagger]
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // [SERVICES: Database — EF Core + PostgreSQL + Redis + Health checks]
    builder.Services.AddInfrastructure(builder.Configuration);

    // [SERVICES: FluentValidation]

    // [SERVICES: Mapster]

    // [SERVICES: Application services / repositories]

    var app = builder.Build();

    // [MIDDLEWARE: Exception handler + Correlation ID — must be first]
    app.UseGlobalExceptionHandler();
    app.UseCorrelationId();

    // [MIDDLEWARE: Serilog request logging]
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} " +
            "in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("CorrelationId",
                httpContext.Response.Headers["X-Correlation-Id"].ToString());
        };
    });

    // [MIDDLEWARE: Scalar UI]
    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerUI();
        app.MapScalarApiReference();
    }

    // [MIDDLEWARE: HTTPS + Auth placeholder]
    app.UseHttpsRedirection();
    app.MapHealthChecks("/health");
    app.UseAuthorization();

    // [ENDPOINTS]
    app.MapControllers();

    Log.Information("Starting ProductManagement API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.Information("Stopped cleanly");
    Log.CloseAndFlush();
}
