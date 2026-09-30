using Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Catalog.Infrastructure;

public static class CatalogDatabaseInitializer
{
    private static readonly string[] CategoryNames =
    [
        "Books", "Computers", "Electronics", "Home", "Kitchen",
        "Office", "Outdoors", "Sports", "Toys", "Wearables"
    ];

    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<CatalogDbContext>>();

        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!options.Enabled || options.ProductCount == 0 ||
            await dbContext.Products.AsNoTracking().AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = await EnsureCategoriesAsync(dbContext, cancellationToken);
        var suppliers = await EnsureSuppliersAsync(dbContext, cancellationToken);
        var createdAtUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        logger.LogInformation("Seeding {ProductCount} deterministic catalog products", options.ProductCount);

        for (var start = 1; start <= options.ProductCount; start += options.BatchSize)
        {
            var count = Math.Min(options.BatchSize, options.ProductCount - start + 1);
            var products = new List<Product>(count);

            for (var sequence = start; sequence < start + count; sequence++)
            {
                var categoryId = categories[(sequence - 1) % categories.Count];
                var supplierId = suppliers[(sequence - 1) % suppliers.Count];
                var price = (((sequence * 7_919L) % 990_000) + 100) / 100m;
                var rating = ((sequence * 37) % 501) / 100m;
                var productCreatedAtUtc = createdAtUtc.AddMinutes(sequence % 525_600);

                products.Add(new Product(
                    $"Product {sequence:D6}",
                    $"Deterministic catalog product {sequence:D6} for repeatable performance tests.",
                    categoryId,
                    supplierId,
                    price,
                    (sequence * 17) % 1_001,
                    rating,
                    productCreatedAtUtc,
                    productCreatedAtUtc.AddDays(sequence % 30)));
            }

            await dbContext.Products.AddRangeAsync(products, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
        }

        logger.LogInformation("Catalog seed completed with {ProductCount} products", options.ProductCount);
    }

    private static async Task<IReadOnlyList<int>> EnsureCategoriesAsync(
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(cancellationToken))
        {
            dbContext.Categories.AddRange(CategoryNames.Select(name => new Category(name)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Id)
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<int>> EnsureSuppliersAsync(
        CatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Suppliers.AnyAsync(cancellationToken))
        {
            dbContext.Suppliers.AddRange(
                Enumerable.Range(1, 50).Select(index => new Supplier($"Supplier {index:D3}")));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Id)
            .Select(supplier => supplier.Id)
            .ToListAsync(cancellationToken);
    }
}
