using MediatR;
using NSubstitute;
using RescuePC.Software.Caching;
using RescuePC.Software.Caching.Behaviors;

namespace RescuePC.Software.Caching.UnitTests.Behaviors;

public class CachingBehaviorTests
{
    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();
    private readonly CachingBehavior<TestRequest, string> _sut;

    public CachingBehaviorTests()
    {
        _sut = new CachingBehavior<TestRequest, string>(_cacheProvider);
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotCacheable_ShouldCallNextWithoutCacheInteraction()
    {
        var request = new NonCacheableRequest();
        var behavior = new CachingBehavior<NonCacheableRequest, string>(_cacheProvider);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("result");

        var result = await behavior.Handle(request, next, CancellationToken.None);

        Assert.Equal("result", result);
        await _cacheProvider.DidNotReceiveWithAnyArgs().GetAsync<string>(default!);
        await _cacheProvider.DidNotReceiveWithAnyArgs().SetAsync<string>(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedValueWithoutCallingNext()
    {
        var request = new TestRequest();
        _cacheProvider.GetAsync<string>(request.CacheKey).Returns("cached");
        var nextCalled = false;
        RequestHandlerDelegate<string> next = _ => { nextCalled = true; return Task.FromResult("fresh"); };

        var result = await _sut.Handle(request, next, CancellationToken.None);

        Assert.Equal("cached", result);
        Assert.False(nextCalled);
        await _cacheProvider.DidNotReceiveWithAnyArgs().SetAsync<string>(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldCallNextAndStoreResultInCache()
    {
        var request = new TestRequest();
        _cacheProvider.GetAsync<string>(request.CacheKey).Returns((string?)null);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("fresh");

        var result = await _sut.Handle(request, next, CancellationToken.None);

        Assert.Equal("fresh", result);
        await _cacheProvider.Received(1).SetAsync(request.CacheKey, "fresh", request.Ttl);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldPassTtlFromRequestToCache()
    {
        var request = new TestRequest { CustomTtl = TimeSpan.FromMinutes(10) };
        _cacheProvider.GetAsync<string>(request.CacheKey).Returns((string?)null);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("value");

        await _sut.Handle(request, next, CancellationToken.None);

        await _cacheProvider.Received(1).SetAsync(request.CacheKey, "value", TimeSpan.FromMinutes(10));
    }

    // --- Helpers ---

    private sealed class TestRequest : ICacheable
    {
        public string CacheKey => "test-key";
        public TimeSpan? Ttl => CustomTtl;
        public TimeSpan? CustomTtl { get; set; }
    }

    private sealed class NonCacheableRequest { }
}
