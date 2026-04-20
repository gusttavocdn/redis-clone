namespace codecrafters_redis;

public sealed class RedisStore(TimeProvider? timeProvider = null) : IRedisStore
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<string, (string Value, DateTimeOffset? ExpiresAt)> _data = new();
    private readonly Dictionary<string, List<StreamEntry>> _streams = new();

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

    public string KeyType(string key)
    {
        if (_streams.ContainsKey(key)) return "stream";
        if (Get(key) is not null)      return "string";
        return "none";
    }

    public string XAdd(string key, string requestedId, IReadOnlyList<(string Field, string Value)> fields)
    {
        var (lastMs, lastSeq) = _streams.TryGetValue(key, out var entries) && entries.Count > 0
            ? ParseId(entries[^1].Id)
            : (0UL, 0UL);

        ulong ms, seq;

        if (requestedId == "*")
        {
            ms  = (ulong)_time.GetUtcNow().ToUnixTimeMilliseconds();
            seq = ms > lastMs ? 0UL : lastSeq + 1;
        }
        else
        {
            var dash = requestedId.IndexOf('-');
            ms = ulong.Parse(requestedId[..dash]);
            var seqPart = requestedId[(dash + 1)..];

            if (seqPart == "*")
                seq = ms > lastMs ? 0UL : lastSeq + 1;
            else
                seq = ulong.Parse(seqPart);
        }

        if (ms == 0 && seq == 0 || ms < lastMs || (ms == lastMs && seq <= lastSeq))
            throw new InvalidOperationException(
                "ERR The ID specified in XADD is equal or smaller than the target stream top item");

        var finalId = $"{ms}-{seq}";

        if (!_streams.TryGetValue(key, out var list))
        {
            list = [];
            _streams[key] = list;
        }

        list.Add(new StreamEntry(finalId, fields));
        return finalId;
    }

    private static (ulong Ms, ulong Seq) ParseId(string id)
    {
        var dash = id.IndexOf('-');
        return (ulong.Parse(id[..dash]), ulong.Parse(id[(dash + 1)..]));
    }
}
