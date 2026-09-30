namespace Catalog.Application;

public sealed record ProductDetails(
    int Id,
    string Name,
    string Description,
    int CategoryId,
    string CategoryName,
    int SupplierId,
    string SupplierName,
    decimal Price,
    int StockQuantity,
    decimal Rating,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ProductSummary(
    int Id,
    string Name,
    string CategoryName,
    string SupplierName,
    decimal Price,
    int StockQuantity,
    decimal Rating);

public sealed record OffsetPage<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    bool HasNextPage);

public sealed record CursorPage<T>(
    IReadOnlyList<T> Items,
    int PageSize,
    int? NextAfterId);

public sealed record ProductSearch(
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinRating,
    int Page,
    int PageSize);

public sealed record ValidationProblem(string Field, string Message);

public static class QueryValidation
{
    public const int DefaultPageSize = 50;
    public const int MaxPage = 1_000_000;
    public const int MaxPageSize = 200;
    public const int MaxTopProducts = 100;

    public static IReadOnlyList<ValidationProblem> ValidateOffset(int page, int pageSize)
    {
        var errors = new List<ValidationProblem>();

        if (page is < 1 or > MaxPage)
        {
            errors.Add(new(nameof(page), $"Page must be between 1 and {MaxPage}."));
        }

        ValidatePageSize(pageSize, errors);
        return errors;
    }

    public static IReadOnlyList<ValidationProblem> ValidateKeyset(int afterId, int pageSize)
    {
        var errors = new List<ValidationProblem>();

        if (afterId < 0)
        {
            errors.Add(new(nameof(afterId), "AfterId must be greater than or equal to 0."));
        }

        ValidatePageSize(pageSize, errors);
        return errors;
    }

    public static IReadOnlyList<ValidationProblem> ValidateSearch(ProductSearch search)
    {
        var errors = ValidateOffset(search.Page, search.PageSize).ToList();

        if (search.CategoryId is <= 0)
        {
            errors.Add(new(nameof(search.CategoryId), "CategoryId must be greater than 0."));
        }

        if (search.MinPrice is < 0)
        {
            errors.Add(new(nameof(search.MinPrice), "MinPrice must be greater than or equal to 0."));
        }

        if (search.MaxPrice is < 0)
        {
            errors.Add(new(nameof(search.MaxPrice), "MaxPrice must be greater than or equal to 0."));
        }

        if (search.MinPrice.HasValue && search.MaxPrice.HasValue && search.MinPrice > search.MaxPrice)
        {
            errors.Add(new(nameof(search.MaxPrice), "MaxPrice must be greater than or equal to MinPrice."));
        }

        if (search.MinRating is < 0 or > 5)
        {
            errors.Add(new(nameof(search.MinRating), "MinRating must be between 0 and 5."));
        }

        return errors;
    }

    public static IReadOnlyList<ValidationProblem> ValidateTake(int take) =>
        take is >= 1 and <= MaxTopProducts
            ? []
            : [new(nameof(take), $"Take must be between 1 and {MaxTopProducts}.")];

    private static void ValidatePageSize(int pageSize, ICollection<ValidationProblem> errors)
    {
        if (pageSize is < 1 or > MaxPageSize)
        {
            errors.Add(new(nameof(pageSize), $"PageSize must be between 1 and {MaxPageSize}."));
        }
    }
}
