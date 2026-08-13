using Microsoft.Extensions.DependencyInjection;
using RescuePC.Software.Caching.Providers.Redis;
using StackExchange.Redis;

namespace RescuePC.Software.Caching;

public static class ServiceCollectionExtensions
{
    public static void AddRedisAsDefaultCacheProvider(this IServiceCollection services, RedisCacheSettings settings)
    {
        var configurationOptions = ConfigurationOptions.Parse(settings.ConnectionString);

        if (!string.IsNullOrEmpty(settings.Password))
        {
            configurationOptions.Password = settings.Password;
        }

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(configurationOptions));
        services.AddSingleton(settings);
        services.AddSingleton<ICacheProvider, RedisCacheProvider>();
    }
}
