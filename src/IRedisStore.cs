namespace codecrafters_redis;

public interface IRedisStore
{
    void Set(string key, string value, TimeSpan? expiry = null);
    string? Get(string key);
    string KeyType(string key);
}
