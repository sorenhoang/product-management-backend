using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ProductManagement.API.Extensions;
using ProductManagement.Application.Extensions;
using ProductManagement.Infrastructure.Extensions;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

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

    // Controllers + API versioning
    builder.Services.AddControllers();
    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion                   = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions                   = true;
            options.ApiVersionReader                    = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat           = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    // OpenAPI / Scalar
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Infrastructure — EF Core + PostgreSQL + Redis + health checks
    builder.Services.AddInfrastructure(builder.Configuration);

    // Application — validators + services
    builder.Services.AddApplication();

    // Mapster
    builder.Services.AddMapster();

    // Rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("stock-adjust", limiterOptions =>
        {
            limiterOptions.Window               = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit          = 30;
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit           = 5;
        });

        options.AddFixedWindowLimiter("write-operations", limiterOptions =>
        {
            limiterOptions.Window               = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit          = 60;
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit           = 10;
        });

        options.AddFixedWindowLimiter("read-operations", limiterOptions =>
        {
            limiterOptions.Window               = TimeSpan.FromMinutes(1);
            limiterOptions.PermitLimit          = 200;
            limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit           = 20;
        });

        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Type     = "https://tools.ietf.org/html/rfc6585#section-4",
                Title    = "Too Many Requests",
                Status   = StatusCodes.Status429TooManyRequests,
                Detail   = "You have exceeded the allowed request rate. " +
                           "Please slow down and try again later.",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            {
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)retryAfter.TotalSeconds).ToString();
                problem.Extensions["retryAfter"] = (int)retryAfter.TotalSeconds;
            }

            await context.HttpContext.Response.WriteAsJsonAsync(problem, ct);
        };
    });

    var app = builder.Build();

    app.UseGlobalExceptionHandler();
    app.UseValidationExceptionHandler();
    app.UseCorrelationId();

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} " +
            "in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost",    httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme",  httpContext.Request.Scheme);
            diagnosticContext.Set("CorrelationId",
                httpContext.Response.Headers["X-Correlation-Id"].ToString());
        };
    });

    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwaggerUI();
        app.MapScalarApiReference();
    }

    app.MapHealthChecks("/health");
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
