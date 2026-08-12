using NSubstitute;
using RescuePC.Software.Caching.Providers.Redis;
using StackExchange.Redis;
using System.Text.Json;

namespace RescuePC.Software.Caching.UnitTests.Providers.Redis;

public class RedisCacheProviderTests
{
    private readonly IDatabase _database = Substitute.For<IDatabase>();
    private readonly RedisCacheSettings _settings = new()
    {
        ConnectionString = "localhost",
        DefaultExpiration = TimeSpan.FromMinutes(5)
    };
    private readonly RedisCacheProvider _sut;

    public RedisCacheProviderTests()
    {
        var multiplexer = Substitute.For<IConnectionMultiplexer>();
        multiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_database);
        _sut = new RedisCacheProvider(multiplexer, _settings);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnDeserializedValue()
    {
        var expected = new SampleData(42, "hello");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(expected);
        _database.StringGetAsync("key", Arg.Any<CommandFlags>()).Returns(bytes);

        var result = await _sut.GetAsync<SampleData>("key");

        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnDefault()
    {
        _database.StringGetAsync("missing", Arg.Any<CommandFlags>()).Returns(RedisValue.Null);

        var result = await _sut.GetAsync<SampleData>("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WhenTtlProvided_ShouldUseProvidedTtl()
    {
        var data = new SampleData(1, "test");
        var ttl = TimeSpan.FromSeconds(30);

        await _sut.SetAsync("key", data, ttl);

        var call = _database.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IDatabase.StringSetAsync));
        var expiry = (Expiration)call.GetArguments()[2]!;
        Assert.Equal(new Expiration(ttl), expiry);
    }

    [Fact]
    public async Task SetAsync_WhenTtlIsNull_ShouldUseDefaultExpiration()
    {
        var data = new SampleData(1, "test");

        await _sut.SetAsync("key", data, null);

        var call = _database.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IDatabase.StringSetAsync));
        var expiry = (Expiration)call.GetArguments()[2]!;
        Assert.Equal(new Expiration(_settings.DefaultExpiration), expiry);
    }

    [Fact]
    public async Task RemoveAsync_ShouldCallKeyDeleteOnDatabase()
    {
        await _sut.RemoveAsync("key");

        await _database.Received(1).KeyDeleteAsync("key", Arg.Any<CommandFlags>());
    }

    // --- Helpers ---

    private sealed record SampleData(int Id, string Name);
}
