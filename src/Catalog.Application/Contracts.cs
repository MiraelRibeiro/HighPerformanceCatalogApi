namespace Catalog.Application;

public interface IProductReadRepository
{
    Task<ProductDetails?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<OffsetPage<ProductSummary>> GetOffsetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<CursorPage<ProductSummary>> GetKeysetPageAsync(int afterId, int pageSize, CancellationToken cancellationToken);
    Task<OffsetPage<ProductSummary>> SearchAsync(ProductSearch search, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSummary>> GetTopAsync(int take, CancellationToken cancellationToken);
}

public interface IProductCache
{
    Task<ProductDetails?> GetAsync(int productId, CancellationToken cancellationToken);
    Task SetAsync(ProductDetails product, CancellationToken cancellationToken);
}

public interface IProductQueries
{
    Task<ProductDetails?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<OffsetPage<ProductSummary>> GetOffsetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<CursorPage<ProductSummary>> GetKeysetPageAsync(int afterId, int pageSize, CancellationToken cancellationToken);
    Task<OffsetPage<ProductSummary>> SearchAsync(ProductSearch search, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductSummary>> GetTopAsync(int take, CancellationToken cancellationToken);
}
