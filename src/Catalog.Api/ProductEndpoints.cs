using Catalog.Application;

namespace Catalog.Api;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/{id:int}", GetByIdAsync).WithName("GetProductById");
        group.MapGet("/", GetProductsAsync).WithName("GetProducts");
        group.MapGet("/search", SearchAsync).WithName("SearchProducts");
        group.MapGet("/top", GetTopAsync)
            .WithName("GetTopProducts")
            .CacheOutput("TopProducts");

        return endpoints;
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        IProductQueries queries,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Validation([new(nameof(id), "Id must be greater than 0.")]);
        }

        var product = await queries.GetByIdAsync(id, cancellationToken);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> GetProductsAsync(
        int? page,
        int? pageSize,
        int? afterId,
        IProductQueries queries,
        CancellationToken cancellationToken)
    {
        var requestedPage = page ?? 1;
        var requestedPageSize = pageSize ?? QueryValidation.DefaultPageSize;

        if (afterId.HasValue)
        {
            var keysetErrors = QueryValidation.ValidateKeyset(afterId.Value, requestedPageSize);
            if (keysetErrors.Count > 0)
            {
                return Validation(keysetErrors);
            }

            if (requestedPage != 1)
            {
                return Validation([new(nameof(page), "Page cannot be combined with AfterId.")]);
            }

            var cursorPage = await queries.GetKeysetPageAsync(
                afterId.Value,
                requestedPageSize,
                cancellationToken);
            return Results.Ok(cursorPage);
        }

        var offsetErrors = QueryValidation.ValidateOffset(requestedPage, requestedPageSize);
        if (offsetErrors.Count > 0)
        {
            return Validation(offsetErrors);
        }

        var result = await queries.GetOffsetPageAsync(requestedPage, requestedPageSize, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SearchAsync(
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        decimal? minRating,
        int? page,
        int? pageSize,
        IProductQueries queries,
        CancellationToken cancellationToken)
    {
        var search = new ProductSearch(
            categoryId,
            minPrice,
            maxPrice,
            minRating,
            page ?? 1,
            pageSize ?? QueryValidation.DefaultPageSize);
        var errors = QueryValidation.ValidateSearch(search);

        if (errors.Count > 0)
        {
            return Validation(errors);
        }

        return Results.Ok(await queries.SearchAsync(search, cancellationToken));
    }

    private static async Task<IResult> GetTopAsync(
        int? take,
        IProductQueries queries,
        CancellationToken cancellationToken)
    {
        var requestedTake = take ?? 20;
        var errors = QueryValidation.ValidateTake(requestedTake);
        return errors.Count > 0
            ? Validation(errors)
            : Results.Ok(await queries.GetTopAsync(requestedTake, cancellationToken));
    }

    private static IResult Validation(IReadOnlyList<ValidationProblem> problems)
    {
        var errors = problems
            .GroupBy(problem => problem.Field)
            .ToDictionary(
                group => group.Key,
                group => group.Select(problem => problem.Message).ToArray());

        return Results.ValidationProblem(errors);
    }
}
