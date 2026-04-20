namespace codecrafters_redis;

public sealed class RedisStore(TimeProvider? timeProvider = null) : IRedisStore
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _data = new();

    public void Set(string key, string value, TimeSpan? expiry = null)
    {
        var expiresAt = expiry.HasValue ? _time.GetUtcNow() + expiry.Value : (DateTimeOffset?)null;
        _data[key] = (value, expiresAt);
    }

    public string? Get(string key)
    {
        if (!_data.TryGetValue(key, out var entry))
            return null;

        if (entry.ExpiresAt.HasValue && _time.GetUtcNow() >= entry.ExpiresAt.Value)
        {
            _data.Remove(key);
            return null;
        }

        return entry.Value;
    }

    public string KeyType(string key) => Get(key) is not null ? "string" : "none";
}
