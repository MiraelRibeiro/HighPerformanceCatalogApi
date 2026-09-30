using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Catalog.IntegrationTests;

public sealed class CatalogApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("catalog_tests")
        .WithUsername("catalog")
        .WithPassword("catalog_test_password")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string RedisConnectionString => _redis.GetConnectionString();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Postgres", _postgres.GetConnectionString());
                builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
                builder.UseSetting("Seed:Enabled", "true");
                builder.UseSetting("Seed:ProductCount", "120");
                builder.UseSetting("Seed:BatchSize", "100");
                builder.UseSetting("Cache:ProductTtlSeconds", "60");
                builder.UseSetting("OutputCache:TopProductsSeconds", "60");
            });

        using var client = Factory.CreateClient();
        using var response = await client.GetAsync("/health", CancellationToken.None);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await Task.WhenAll(_postgres.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }
}

[CollectionDefinition(Name)]
public sealed class CatalogApiCollection : ICollectionFixture<CatalogApiFixture>
{
    public const string Name = "catalog-api";
}
