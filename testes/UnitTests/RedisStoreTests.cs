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

    [Fact]
    public void KeyType_StreamKey_ReturnsStream()
    {
        _store.XAdd("mystream", "1-1", [("name", "John")]);

        _store.KeyType("mystream").Should().Be("stream");
    }

    [Fact]
    public void XAdd_FullId_UsesExactId()
    {
        var id = _store.XAdd("s", "1000-5", [("f", "v")]);

        id.Should().Be("1000-5");
    }

    [Fact]
    public void XAdd_FullId_ZeroOne_Succeeds()
    {
        var id = _store.XAdd("s", "0-1", [("f", "v")]);

        id.Should().Be("0-1");
    }

    [Fact]
    public void XAdd_FullId_ZeroZero_ThrowsInvalidOperation()
    {
        var act = () => _store.XAdd("s", "0-0", [("f", "v")]);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("ERR The ID specified in XADD is equal or smaller than the target stream top item");
    }

    [Fact]
    public void XAdd_FullId_SameAsLast_ThrowsInvalidOperation()
    {
        _store.XAdd("s", "1000-5", [("f", "v")]);

        var act = () => _store.XAdd("s", "1000-5", [("f", "v")]);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("ERR The ID specified in XADD is equal or smaller than the target stream top item");
    }

    [Fact]
    public void XAdd_FullId_LessThanLast_ThrowsInvalidOperation()
    {
        _store.XAdd("s", "1000-5", [("f", "v")]);

        var act = () => _store.XAdd("s", "999-0", [("f", "v")]);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("ERR The ID specified in XADD is equal or smaller than the target stream top item");
    }

    [Fact]
    public void XAdd_PartialId_FirstEntry_SeqIsZero()
    {
        var id = _store.XAdd("s", "1000-*", [("f", "v")]);

        id.Should().Be("1000-0");
    }

    [Fact]
    public void XAdd_PartialId_SameMs_IncrementsSeq()
    {
        _store.XAdd("s", "1000-5", [("f", "v")]);

        var id = _store.XAdd("s", "1000-*", [("f", "v")]);

        id.Should().Be("1000-6");
    }

    [Fact]
    public void XAdd_AutoId_ReturnsIdWithCurrentMs()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);
        var expectedMs = fakeTime.GetUtcNow().ToUnixTimeMilliseconds();

        var id = store.XAdd("s", "*", [("f", "v")]);

        id.Should().StartWith($"{expectedMs}-");
    }

    [Fact]
    public void XAdd_AutoId_SecondCallSameMs_IncrementsSeq()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.XAdd("s", "*", [("f", "v")]);
        var id = store.XAdd("s", "*", [("f", "v")]);

        id.Should().EndWith("-1");
    }

    [Fact]
    public void XAdd_AutoId_AfterTimeAdvance_ResetsSeqToZero()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        store.XAdd("s", "*", [("f", "v")]);
        fakeTime.Advance(TimeSpan.FromMilliseconds(1));
        var id = store.XAdd("s", "*", [("f", "v")]);

        id.Should().EndWith("-0");
    }
}
