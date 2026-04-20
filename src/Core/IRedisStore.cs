namespace codecrafters_redis;

public interface IRedisStore
{
    void Set(string key, string value, TimeSpan? expiry = null);
    string? Get(string key);
    string KeyType(string key);
    Result<string> XAdd(string key, string requestedId, IReadOnlyList<(string Field, string Value)> fields);
    IReadOnlyList<StreamEntry> XRange(string key, string start, string end);
}
