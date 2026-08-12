namespace RescuePC.Software.Caching.Providers.Redis;

public class RedisCacheSettings
{
    public required string ConnectionString { get; init; }
    public string? Password { get; init; }
    public TimeSpan DefaultExpiration { get; init; }
}
