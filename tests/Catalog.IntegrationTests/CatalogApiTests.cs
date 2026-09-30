using System.Net;
using System.Net.Http.Json;
using Catalog.Application;
using Catalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Catalog.IntegrationTests;

[Collection(CatalogApiCollection.Name)]
public sealed class CatalogApiTests(CatalogApiFixture fixture)
{
    [Fact]
    public async Task GetProduct_WhenProductExists_ReturnsProjectedProduct()
    {
        using var client = fixture.Factory.CreateClient();

        var product = await client.GetFromJsonAsync<ProductDetails>(
            "/api/products/1",
            CancellationToken.None);

        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
        Assert.NotEmpty(product.CategoryName);
        Assert.NotEmpty(product.SupplierName);
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_ReturnsNotFound()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/products/999999",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WhenPageSizeExceedsLimit_ReturnsValidationError()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/products?page=1&pageSize=201",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithOffsetAndKeyset_ReturnsStablePages()
    {
        using var client = fixture.Factory.CreateClient();

        var offset = await client.GetFromJsonAsync<OffsetPage<ProductSummary>>(
            "/api/products?page=1&pageSize=10",
            CancellationToken.None);
        var afterId = offset!.Items[^1].Id;
        var keyset = await client.GetFromJsonAsync<CursorPage<ProductSummary>>(
            $"/api/products?afterId={afterId}&pageSize=10",
            CancellationToken.None);

        Assert.Equal(10, offset.Items.Count);
        Assert.True(offset.HasNextPage);
        Assert.Equal(10, keyset!.Items.Count);
        Assert.All(keyset.Items, product => Assert.True(product.Id > afterId));
    }

    [Fact]
    public async Task SearchProducts_WhenFiltersAreProvided_ReturnsMatchingProducts()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync(
            "/api/products/search?categoryId=1&minPrice=10&maxPrice=9000&minRating=1&pageSize=20",
            CancellationToken.None);
        var responseBody = await response.Content.ReadAsStringAsync(CancellationToken.None);
        Assert.True(response.IsSuccessStatusCode, responseBody);
        var result = await response.Content.ReadFromJsonAsync<OffsetPage<ProductSummary>>(
            cancellationToken: CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, product =>
        {
            Assert.InRange(product.Price, 10m, 9000m);
            Assert.True(product.Rating >= 1m);
        });
    }

    [Fact]
    public async Task GetProduct_WhenCacheMiss_LoadsDatabaseAndCachesWithTtl()
    {
        using var client = fixture.Factory.CreateClient();
        var first = await client.GetFromJsonAsync<ProductDetails>(
            "/api/products/2",
            CancellationToken.None);

        await using var redis = await ConnectionMultiplexer.ConnectAsync(fixture.RedisConnectionString);
        var ttl = await redis.GetDatabase().KeyTimeToLiveAsync("catalog:product:v1:2");

        Assert.NotNull(first);
        Assert.NotNull(ttl);
        Assert.InRange(ttl.Value, TimeSpan.Zero, TimeSpan.FromSeconds(60));

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await dbContext.Products
                .Where(product => product.Id == 2)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(product => product.Name, "Changed in database"),
                    CancellationToken.None);
        }

        var second = await client.GetFromJsonAsync<ProductDetails>(
            "/api/products/2",
            CancellationToken.None);

        Assert.Equal(first.Name, second!.Name);
    }

    [Fact]
    public async Task GetTopProducts_WhenCalledTwice_ReusesOutputCache()
    {
        using var client = fixture.Factory.CreateClient();
        var first = await client.GetFromJsonAsync<IReadOnlyList<ProductSummary>>(
            "/api/products/top?take=5",
            CancellationToken.None);
        Assert.NotNull(first);

        var firstProduct = first[0];
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await dbContext.Products
                .Where(product => product.Id == firstProduct.Id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(product => product.Name, "Changed after output cache"),
                    CancellationToken.None);
        }

        var second = await client.GetFromJsonAsync<IReadOnlyList<ProductSummary>>(
            "/api/products/top?take=5",
            CancellationToken.None);

        Assert.Equal(firstProduct.Name, second![0].Name);
    }

    [Fact]
    public async Task Health_WhenDependenciesAreAvailable_ReturnsOk()
    {
        using var client = fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/health", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
