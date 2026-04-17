using FluentAssertions;

namespace codecrafters_redis.UnitTests;

public class RedisStoreTests
{
    private readonly RedisStore _store = new();

    [Fact]
    public void Set_ThenGet_ReturnsValue()
    {
        _store.Set("foo", "bar");

        _store.Get("foo").Should().Be("bar");
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsNull()
    {
        _store.Get("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        _store.Set("foo", "bar");
        _store.Set("foo", "baz");

        _store.Get("foo").Should().Be("baz");
    }
}
