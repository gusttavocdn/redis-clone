namespace codecrafters_redis;

public interface IRedisStore
{
    void Set(string key, string value);
    string? Get(string key);
}
