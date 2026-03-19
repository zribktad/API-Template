using APITemplate.Application.Common.Contracts;
using APITemplate.Infrastructure.Idempotency;
using StackExchange.Redis;

namespace APITemplate.Extensions;

public static class IdempotencyServiceCollectionExtensions
{
    public static IServiceCollection AddIdempotencyStore(this IServiceCollection services)
    {
        services.AddSingleton<IIdempotencyStore>(sp =>
        {
            var multiplexer = sp.GetService<IConnectionMultiplexer>();
            if (multiplexer is not null)
                return new DistributedCacheIdempotencyStore(multiplexer);

            return new InMemoryIdempotencyStore(sp.GetRequiredService<TimeProvider>());
        });

        return services;
    }
}
