namespace codecrafters_redis;

internal sealed class StreamStore(TimeProvider time)
{
    private readonly Dictionary<string, List<StreamEntry>> _data = new();

    public bool HasKey(string key) => _data.ContainsKey(key);

    public Result<string> XAdd(string key, string requestedId, IReadOnlyList<(string Field, string Value)> fields)
    {
        var lastId = _data.TryGetValue(key, out var entries) && entries.Count > 0
            ? StreamId.Parse(entries[^1].Id)
            : new StreamId(0, 0);

        var (ms, seq) = ResolveId(requestedId, lastId);
        var newId = new StreamId(ms, seq);

        if (!newId.IsGreaterThan(lastId))
            return Result<string>.Fail(
                "ERR The ID specified in XADD is equal or smaller than the target stream top item");

        var finalId = newId.ToString();

        if (!_data.TryGetValue(key, out var list))
        {
            list = [];
            _data[key] = list;
        }

        list.Add(new StreamEntry(finalId, fields));
        return Result<string>.Ok(finalId);
    }

    private (ulong Ms, ulong Seq) ResolveId(string requestedId, StreamId lastId)
    {
        if (requestedId == "*")
        {
            var currentMs = (ulong)time.GetUtcNow().ToUnixTimeMilliseconds();
            var ms = currentMs >= lastId.Ms ? currentMs : lastId.Ms;
            return (ms, ms > lastId.Ms ? 0UL : lastId.Seq + 1);
        }

        var dash     = requestedId.IndexOf('-');
        var parsedMs = ulong.Parse(requestedId[..dash]);
        var seqPart  = requestedId[(dash + 1)..];

        return seqPart == "*"
            ? (parsedMs, parsedMs > lastId.Ms ? 0UL : lastId.Seq + 1)
            : (parsedMs, ulong.Parse(seqPart));
    }
}
