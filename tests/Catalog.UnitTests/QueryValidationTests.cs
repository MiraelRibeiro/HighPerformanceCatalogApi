using Catalog.Application;

namespace Catalog.UnitTests;

public sealed class QueryValidationTests
{
    [Theory]
    [InlineData(0, 50, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, 201, "pageSize")]
    public void ValidateOffset_WhenPagingIsInvalid_ReturnsExpectedError(
        int page,
        int pageSize,
        string field)
    {
        var errors = QueryValidation.ValidateOffset(page, pageSize);

        Assert.Contains(errors, error => error.Field == field);
    }

    [Fact]
    public void ValidateSearch_WhenPriceRangeIsReversed_ReturnsValidationError()
    {
        var search = new ProductSearch(1, 100m, 10m, 4m, 1, 50);

        var errors = QueryValidation.ValidateSearch(search);

        Assert.Contains(errors, error => error.Field == "MaxPrice");
    }

    [Fact]
    public void ValidateSearch_WhenValuesAreValid_ReturnsNoErrors()
    {
        var search = new ProductSearch(1, 10m, 500m, 3.5m, 2, 50);

        var errors = QueryValidation.ValidateSearch(search);

        Assert.Empty(errors);
    }
}
