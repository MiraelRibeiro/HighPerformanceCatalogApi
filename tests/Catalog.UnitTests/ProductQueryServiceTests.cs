using Catalog.Application;

namespace Catalog.UnitTests;

public sealed class ProductQueryServiceTests
{
    private static readonly ProductDetails Product = new(
        1,
        "Product 000001",
        "Description",
        1,
        "Books",
        1,
        "Supplier 001",
        10m,
        20,
        4.5m,
        DateTime.UnixEpoch,
        DateTime.UnixEpoch);

    [Fact]
    public async Task GetProduct_WhenCacheHit_DoesNotQueryRepository()
    {
        var repository = new FakeRepository { Product = Product };
        var cache = new FakeCache { Product = Product };
        var service = new ProductQueryService(repository, cache);

        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.Equal(Product, result);
        Assert.Equal(0, repository.GetByIdCalls);
        Assert.Equal(0, cache.SetCalls);
    }

    [Fact]
    public async Task GetProduct_WhenCacheMiss_LoadsFromRepositoryAndCachesResult()
    {
        var repository = new FakeRepository { Product = Product };
        var cache = new FakeCache();
        var service = new ProductQueryService(repository, cache);

        var result = await service.GetByIdAsync(1, CancellationToken.None);

        Assert.Equal(Product, result);
        Assert.Equal(1, repository.GetByIdCalls);
        Assert.Equal(1, cache.SetCalls);
        Assert.Equal(Product, cache.Product);
    }

    [Fact]
    public async Task GetProduct_WhenProductDoesNotExist_DoesNotCacheNull()
    {
        var repository = new FakeRepository();
        var cache = new FakeCache();
        var service = new ProductQueryService(repository, cache);

        var result = await service.GetByIdAsync(999, CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(1, repository.GetByIdCalls);
        Assert.Equal(0, cache.SetCalls);
    }

    private sealed class FakeCache : IProductCache
    {
        public ProductDetails? Product { get; set; }
        public int SetCalls { get; private set; }

        public Task<ProductDetails?> GetAsync(int productId, CancellationToken cancellationToken) =>
            Task.FromResult(Product);

        public Task SetAsync(ProductDetails product, CancellationToken cancellationToken)
        {
            Product = product;
            SetCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRepository : IProductReadRepository
    {
        public ProductDetails? Product { get; init; }
        public int GetByIdCalls { get; private set; }

        public Task<ProductDetails?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            GetByIdCalls++;
            return Task.FromResult(Product);
        }

        public Task<OffsetPage<ProductSummary>> GetOffsetPageAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<CursorPage<ProductSummary>> GetKeysetPageAsync(
            int afterId,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<OffsetPage<ProductSummary>> SearchAsync(
            ProductSearch search,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<ProductSummary>> GetTopAsync(
            int take,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
