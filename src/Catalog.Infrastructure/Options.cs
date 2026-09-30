namespace Catalog.Infrastructure;

public sealed class RedisCacheOptions
{
    public const string SectionName = "Cache";

    public int ProductTtlSeconds { get; init; } = 60;
}

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; init; } = true;
    public int ProductCount { get; init; } = 100_000;
    public int BatchSize { get; init; } = 2_000;
}
