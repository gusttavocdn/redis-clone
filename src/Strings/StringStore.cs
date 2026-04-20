namespace codecrafters_redis;

internal sealed class StringStore(TimeProvider time)
{
    private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _data = new();

    public void Set(string key, string value, TimeSpan? expiry = null)
    {
        var expiresAt = expiry.HasValue ? time.GetUtcNow() + expiry.Value : (DateTimeOffset?)null;
        _data[key] = (value, expiresAt);
    }

    public string? Get(string key)
    {
        if (!_data.TryGetValue(key, out var entry))
            return null;

        if (entry.ExpiresAt.HasValue && time.GetUtcNow() >= entry.ExpiresAt.Value)
        {
            _data.Remove(key);
            return null;
        }

        return entry.Value;
    }
}
