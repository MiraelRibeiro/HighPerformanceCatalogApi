using System.Text.Json;
using Catalog.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Catalog.Infrastructure;

public sealed class RedisProductCache(
    Task<IConnectionMultiplexer> connectionTask,
    IOptions<RedisCacheOptions> options,
    ILogger<RedisProductCache> logger) : IProductCache
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly TimeSpan _timeToLive = TimeSpan.FromSeconds(options.Value.ProductTtlSeconds);

    public async Task<ProductDetails?> GetAsync(int productId, CancellationToken cancellationToken)
    {
        try
        {
            var connection = await connectionTask.WaitAsync(cancellationToken);
            var value = await connection.GetDatabase()
                .StringGetAsync(GetKey(productId))
                .WaitAsync(cancellationToken);

            return value.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<ProductDetails>((string)value!, SerializerOptions);
        }
        catch (Exception exception) when (exception is RedisException or JsonException)
        {
            logger.LogWarning(exception, "Redis product cache read failed; falling back to PostgreSQL");
            return null;
        }
    }

    public async Task SetAsync(ProductDetails product, CancellationToken cancellationToken)
    {
        try
        {
            var connection = await connectionTask.WaitAsync(cancellationToken);
            var value = JsonSerializer.Serialize(product, SerializerOptions);

            await connection.GetDatabase()
                .StringSetAsync(GetKey(product.Id), value, _timeToLive)
                .WaitAsync(cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis product cache write failed");
        }
    }

    internal static string GetKey(int productId) => $"catalog:product:v1:{productId}";
}
