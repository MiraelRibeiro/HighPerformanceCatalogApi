using Catalog.Application;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure;

public sealed class ProductReadRepository(CatalogDbContext dbContext) : IProductReadRepository
{
    public Task<ProductDetails?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductDetails(
                product.Id,
                product.Name,
                product.Description,
                product.CategoryId,
                product.Category.Name,
                product.SupplierId,
                product.Supplier.Name,
                product.Price,
                product.StockQuantity,
                product.Rating,
                product.CreatedAtUtc,
                product.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<OffsetPage<ProductSummary>> GetOffsetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var items = await ProjectSummaries(dbContext.Products
                .AsNoTracking()
                .OrderBy(product => product.Id))
            .Skip(checked((page - 1) * pageSize))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
        {
            items.RemoveAt(pageSize);
        }

        return new(items, page, pageSize, hasNextPage);
    }

    public async Task<CursorPage<ProductSummary>> GetKeysetPageAsync(
        int afterId,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var items = await ProjectSummaries(dbContext.Products
                .AsNoTracking()
                .Where(product => product.Id > afterId)
                .OrderBy(product => product.Id))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
        {
            items.RemoveAt(pageSize);
        }

        int? nextAfterId = hasNextPage && items.Count > 0 ? items[^1].Id : null;
        return new(items, pageSize, nextAfterId);
    }

    public async Task<OffsetPage<ProductSummary>> SearchAsync(
        ProductSearch search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();

        if (search.CategoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == search.CategoryId);
        }

        if (search.MinPrice.HasValue)
        {
            query = query.Where(product => product.Price >= search.MinPrice);
        }

        if (search.MaxPrice.HasValue)
        {
            query = query.Where(product => product.Price <= search.MaxPrice);
        }

        if (search.MinRating.HasValue)
        {
            query = query.Where(product => product.Rating >= search.MinRating);
        }

        var items = await query
            .OrderBy(product => product.Id)
            .Select(product => new ProductSummary(
                product.Id,
                product.Name,
                product.Category.Name,
                product.Supplier.Name,
                product.Price,
                product.StockQuantity,
                product.Rating))
            .Skip(checked((search.Page - 1) * search.PageSize))
            .Take(search.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > search.PageSize;
        if (hasNextPage)
        {
            items.RemoveAt(search.PageSize);
        }

        return new(items, search.Page, search.PageSize, hasNextPage);
    }

    public async Task<IReadOnlyList<ProductSummary>> GetTopAsync(
        int take,
        CancellationToken cancellationToken) =>
        await ProjectSummaries(dbContext.Products
                .AsNoTracking()
                .OrderByDescending(product => product.Rating)
                .ThenBy(product => product.Id))
            .Take(take)
            .ToListAsync(cancellationToken);

    private static IQueryable<ProductSummary> ProjectSummaries(IQueryable<Catalog.Domain.Product> query) =>
        query
            .Select(product => new ProductSummary(
                product.Id,
                product.Name,
                product.Category.Name,
                product.Supplier.Name,
                product.Price,
                product.StockQuantity,
                product.Rating));
}
