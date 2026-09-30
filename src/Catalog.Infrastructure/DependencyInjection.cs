using Catalog.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackExchange.Redis;

namespace Catalog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");

        var dataSource = new NpgsqlDataSourceBuilder(postgresConnectionString).Build();
        services.AddSingleton(dataSource);
        services.AddDbContextPool<CatalogDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                npgsql => npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null)));

        var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
        redisConfiguration.AbortOnConnectFail = false;
        redisConfiguration.ConnectRetry = 3;
        services.AddSingleton(_ => ConnectRedisAsync(redisConfiguration));

        services.AddOptions<RedisCacheOptions>()
            .Bind(configuration.GetSection(RedisCacheOptions.SectionName))
            .Validate(options => options.ProductTtlSeconds is >= 1 and <= 3_600,
                "Cache:ProductTtlSeconds must be between 1 and 3600.")
            .ValidateOnStart();

        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .Validate(options => options.ProductCount is >= 0 and <= 1_000_000,
                "Seed:ProductCount must be between 0 and 1000000.")
            .Validate(options => options.BatchSize is >= 100 and <= 10_000,
                "Seed:BatchSize must be between 100 and 10000.")
            .ValidateOnStart();

        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddSingleton<IProductCache, RedisProductCache>();
        services.AddScoped<IProductQueries, ProductQueryService>();

        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);

        return services;
    }

    private static async Task<IConnectionMultiplexer> ConnectRedisAsync(ConfigurationOptions configuration) =>
        await ConnectionMultiplexer.ConnectAsync(configuration);
}
