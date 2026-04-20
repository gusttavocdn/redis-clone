namespace codecrafters_redis;

public sealed class RedisStore : IRedisStore
{
    private readonly StringStore _strings;
    private readonly StreamStore _streams;

    public RedisStore(TimeProvider? timeProvider = null)
    {
        var time = timeProvider ?? TimeProvider.System;
        _strings = new StringStore(time);
        _streams = new StreamStore(time);
    }

    public void Set(string key, string value, TimeSpan? expiry = null) =>
        _strings.Set(key, value, expiry);

    public string? Get(string key) => _strings.Get(key);

    public string KeyType(string key)
    {
        if (_streams.HasKey(key)) return "stream";
        if (_strings.Get(key) is not null) return "string";
        return "none";
    }

    public Result<string> XAdd(string key, string requestedId, IReadOnlyList<(string Field, string Value)> fields) =>
        _streams.XAdd(key, requestedId, fields);

    public IReadOnlyList<StreamEntry> XRange(string key, string start, string end) =>
        _streams.XRange(key, start, end);
}
