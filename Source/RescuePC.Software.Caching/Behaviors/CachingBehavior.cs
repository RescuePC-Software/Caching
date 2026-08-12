using MediatR;

namespace RescuePC.Software.Caching.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheProvider _cacheProvider;

    public CachingBehavior(ICacheProvider cacheProvider)
    {
        _cacheProvider = cacheProvider;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
        {
            return await next(cancellationToken);
        }

        var cached = await _cacheProvider.GetAsync<TResponse>(cacheable.CacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var response = await next(cancellationToken);

        await _cacheProvider.SetAsync(cacheable.CacheKey, response, cacheable.Ttl);

        return response;
    }
}
