namespace RescuePC.Software.Caching;

public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Ttl { get; }
}
