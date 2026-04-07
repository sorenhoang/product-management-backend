using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Application.Common.Interfaces;
using ProductManagement.Infrastructure.Caching;
using ProductManagement.Infrastructure.Persistence;
using ProductManagement.Infrastructure.Repositories;
using StackExchange.Redis;

namespace ProductManagement.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // Cache settings
        services.Configure<CacheSettings>(configuration.GetSection(CacheSettings.SectionName));

        // Redis — IConnectionMultiplexer must be singleton (SE.Redis requirement)
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                configuration["Redis:ConnectionString"] ?? "localhost:6379"));

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration["Redis:ConnectionString"] ?? "localhost:6379");

        services.AddScoped<ICacheService, RedisCacheService>();

        // Health checks
        services.AddHealthChecks()
            .AddRedis(
                configuration["Redis:ConnectionString"] ?? "localhost:6379",
                name: "redis",
                tags: ["cache"])
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "postgres",
                tags: ["database"]);

        return services;
    }
}
