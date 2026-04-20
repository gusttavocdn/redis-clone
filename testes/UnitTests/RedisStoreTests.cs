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
        _ = _store.XAdd("mystream", "1-1", [("name", "John")]);

        _store.KeyType("mystream").Should().Be("stream");
    }

    [Fact]
    public void XAdd_FullId_UsesExactId()
    {
        _store.XAdd("s", "1000-5", [("f", "v")]).Value.Should().Be("1000-5");
    }

    [Fact]
    public void XAdd_FullId_ZeroOne_Succeeds()
    {
        _store.XAdd("s", "0-1", [("f", "v")]).Value.Should().Be("0-1");
    }

    [Fact]
    public void XAdd_FullId_ZeroZero_ReturnsSpecificError()
    {
        var result = _store.XAdd("s", "0-0", [("f", "v")]);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be(
            "ERR The ID specified in XADD must be greater than 0-0");
    }

    [Fact]
    public void XAdd_FullId_SameAsLast_ReturnsError()
    {
        _ = _store.XAdd("s", "1000-5", [("f", "v")]);

        var result = _store.XAdd("s", "1000-5", [("f", "v")]);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be(
            "ERR The ID specified in XADD is equal or smaller than the target stream top item");
    }

    [Fact]
    public void XAdd_FullId_LessThanLast_ReturnsError()
    {
        _ = _store.XAdd("s", "1000-5", [("f", "v")]);

        var result = _store.XAdd("s", "999-0", [("f", "v")]);

        result.IsError.Should().BeTrue();
        result.Error.Should().Be(
            "ERR The ID specified in XADD is equal or smaller than the target stream top item");
    }

    [Fact]
    public void XAdd_PartialId_FirstEntry_SeqIsZero()
    {
        _store.XAdd("s", "1000-*", [("f", "v")]).Value.Should().Be("1000-0");
    }

    [Fact]
    public void XAdd_PartialId_SameMs_IncrementsSeq()
    {
        _ = _store.XAdd("s", "1000-5", [("f", "v")]);

        _store.XAdd("s", "1000-*", [("f", "v")]).Value.Should().Be("1000-6");
    }

    [Fact]
    public void XAdd_AutoId_ReturnsIdWithCurrentMs()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);
        var expectedMs = fakeTime.GetUtcNow().ToUnixTimeMilliseconds();

        store.XAdd("s", "*", [("f", "v")]).Value.Should().StartWith($"{expectedMs}-");
    }

    [Fact]
    public void XAdd_AutoId_SecondCallSameMs_IncrementsSeq()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        _ = store.XAdd("s", "*", [("f", "v")]);

        store.XAdd("s", "*", [("f", "v")]).Value.Should().EndWith("-1");
    }

    [Fact]
    public void XAdd_AutoId_AfterTimeAdvance_ResetsSeqToZero()
    {
        var fakeTime = new FakeTimeProvider();
        var store = new RedisStore(fakeTime);

        _ = store.XAdd("s", "*", [("f", "v")]);
        fakeTime.Advance(TimeSpan.FromMilliseconds(1));

        store.XAdd("s", "*", [("f", "v")]).Value.Should().EndWith("-0");
    }

    // ── XRange ───────────────────────────────────────────────────────────────

    [Fact]
    public void XRange_NonExistentKey_ReturnsEmpty()
    {
        _store.XRange("missing", "-", "+").Should().BeEmpty();
    }

    [Fact]
    public void XRange_FullRange_ReturnsAllEntries()
    {
        _ = _store.XAdd("s", "1-1", [("f", "v")]);
        _ = _store.XAdd("s", "2-1", [("f", "v")]);

        var result = _store.XRange("s", "-", "+");

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("1-1");
        result[1].Id.Should().Be("2-1");
    }

    [Fact]
    public void XRange_StartFilter_ExcludesEntriesBeforeStart()
    {
        _ = _store.XAdd("s", "1-1", [("f", "v")]);
        _ = _store.XAdd("s", "2-1", [("f", "v")]);
        _ = _store.XAdd("s", "3-1", [("f", "v")]);

        var result = _store.XRange("s", "2-0", "+");

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("2-1");
        result[1].Id.Should().Be("3-1");
    }

    [Fact]
    public void XRange_EndFilter_ExcludesEntriesAfterEnd()
    {
        _ = _store.XAdd("s", "1-1", [("f", "v")]);
        _ = _store.XAdd("s", "2-1", [("f", "v")]);
        _ = _store.XAdd("s", "3-1", [("f", "v")]);

        var result = _store.XRange("s", "-", "2-1");

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("1-1");
        result[1].Id.Should().Be("2-1");
    }

    [Fact]
    public void XRange_ExactId_ReturnsOnlyThatEntry()
    {
        _ = _store.XAdd("s", "1-1", [("f", "v")]);
        _ = _store.XAdd("s", "2-1", [("f", "v")]);
        _ = _store.XAdd("s", "3-1", [("f", "v")]);

        var result = _store.XRange("s", "2-1", "2-1");

        result.Should().ContainSingle().Which.Id.Should().Be("2-1");
    }

    [Fact]
    public void XRange_PartialStartId_SeqCompletedAsZero()
    {
        _ = _store.XAdd("s", "1000-0", [("f", "v")]);
        _ = _store.XAdd("s", "1000-5", [("f", "v")]);

        var result = _store.XRange("s", "1000", "+");

        result.Should().HaveCount(2);
    }

    [Fact]
    public void XRange_PartialEndId_SeqCompletedAsMax()
    {
        _ = _store.XAdd("s", "999-0",  [("f", "v")]);
        _ = _store.XAdd("s", "1000-5", [("f", "v")]);
        _ = _store.XAdd("s", "1001-0", [("f", "v")]);

        var result = _store.XRange("s", "-", "1000");

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("999-0");
        result[1].Id.Should().Be("1000-5");
    }

    [Fact]
    public void XRange_NoMatchInRange_ReturnsEmpty()
    {
        _ = _store.XAdd("s", "1-1", [("f", "v")]);
        _ = _store.XAdd("s", "5-1", [("f", "v")]);

        _store.XRange("s", "2-0", "3-0").Should().BeEmpty();
    }

    [Fact]
    public void XRange_EntryFieldsArePreserved()
    {
        _ = _store.XAdd("s", "1-1", [("name", "John"), ("age", "30")]);

        var result = _store.XRange("s", "-", "+");

        result.Should().ContainSingle();
        result[0].Fields.Should().BeEquivalentTo(
            new[] { ("name", "John"), ("age", "30") },
            o => o.WithStrictOrdering());
    }
}
