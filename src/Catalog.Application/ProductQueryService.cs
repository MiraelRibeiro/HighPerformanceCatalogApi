using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Catalog.Application;

public sealed class ProductQueryService(
    IProductReadRepository repository,
    IProductCache cache) : IProductQueries
{
    public const string ActivitySourceName = "Catalog.Application";
    public const string MeterName = "Catalog.Application";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> CacheHits = Meter.CreateCounter<long>("catalog_cache_hits_total");
    private static readonly Counter<long> CacheMisses = Meter.CreateCounter<long>("catalog_cache_misses_total");
    private static readonly Counter<long> SearchRequests = Meter.CreateCounter<long>("catalog_search_requests_total");

    public async Task<ProductDetails?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("catalog.product.get");

        var cached = await cache.GetAsync(id, cancellationToken);
        if (cached is not null)
        {
            CacheHits.Add(1);
            activity?.SetTag("catalog.cache.hit", true);
            return cached;
        }

        CacheMisses.Add(1);
        activity?.SetTag("catalog.cache.hit", false);

        var product = await repository.GetByIdAsync(id, cancellationToken);
        if (product is not null)
        {
            await cache.SetAsync(product, cancellationToken);
        }

        return product;
    }

    public Task<OffsetPage<ProductSummary>> GetOffsetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        repository.GetOffsetPageAsync(page, pageSize, cancellationToken);

    public Task<CursorPage<ProductSummary>> GetKeysetPageAsync(
        int afterId,
        int pageSize,
        CancellationToken cancellationToken) =>
        repository.GetKeysetPageAsync(afterId, pageSize, cancellationToken);

    public Task<OffsetPage<ProductSummary>> SearchAsync(
        ProductSearch search,
        CancellationToken cancellationToken)
    {
        SearchRequests.Add(1);
        return repository.SearchAsync(search, cancellationToken);
    }

    public Task<IReadOnlyList<ProductSummary>> GetTopAsync(
        int take,
        CancellationToken cancellationToken) =>
        repository.GetTopAsync(take, cancellationToken);
}
