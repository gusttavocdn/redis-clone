namespace codecrafters_redis;

public sealed class RedisStore : IRedisStore
{
    private readonly Dictionary<string, string> _data = new();

    public void Set(string key, string value) => _data[key] = value;

    public string? Get(string key) => _data.TryGetValue(key, out var value) ? value : null;
}
