using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

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

    [Fact]
    public void Get_BeforeEXExpiry_ReturnsValue()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.Set("foo", "bar", TimeSpan.FromSeconds(5));
        fakeTime.Advance(TimeSpan.FromSeconds(4));

        store.Get("foo").Should().Be("bar");
    }

    [Fact]
    public void Get_AfterEXExpiry_ReturnsNull()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.Set("foo", "bar", TimeSpan.FromSeconds(5));
        fakeTime.Advance(TimeSpan.FromSeconds(6));

        store.Get("foo").Should().BeNull();
    }

    [Fact]
    public void Get_AfterPXExpiry_ReturnsNull()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.Set("foo", "bar", TimeSpan.FromMilliseconds(200));
        fakeTime.Advance(TimeSpan.FromMilliseconds(201));

        store.Get("foo").Should().BeNull();
    }

    [Fact]
    public void Get_ExpiredKeyIsEvicted_SubsequentGetReturnsNull()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.Set("foo", "bar", TimeSpan.FromSeconds(1));
        fakeTime.Advance(TimeSpan.FromSeconds(2));

        store.Get("foo");
        store.Get("foo").Should().BeNull();
    }

    [Fact]
    public void Set_WithoutExpiry_NeverExpires()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.Set("foo", "bar");
        fakeTime.Advance(TimeSpan.FromDays(365 * 100));

        store.Get("foo").Should().Be("bar");
    }

    [Fact]
    public void KeyType_ExistingStringKey_ReturnsString()
    {
        _store.Set("foo", "bar");

        _store.KeyType("foo").Should().Be("string");
    }

    [Fact]
    public void KeyType_NonExistentKey_ReturnsNone()
    {
        _store.KeyType("nonexistent").Should().Be("none");
    }

    [Fact]
    public void KeyType_ExpiredKey_ReturnsNone()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.Set("foo", "bar", TimeSpan.FromSeconds(5));
        fakeTime.Advance(TimeSpan.FromSeconds(6));

        store.KeyType("foo").Should().Be("none");
    }
}
