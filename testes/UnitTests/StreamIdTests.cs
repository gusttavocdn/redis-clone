using FluentAssertions;

namespace codecrafters_redis.UnitTests;

public class StreamIdTests
{
    // ── ParseStart ──────────────────────────────────────────────────────────

    [Fact]
    public void ParseStart_Dash_ReturnsMinId()
    {
        StreamId.ParseStart("-").Should().Be(new StreamId(0, 0));
    }

    [Fact]
    public void ParseStart_PartialId_SeqIsZero()
    {
        StreamId.ParseStart("1000").Should().Be(new StreamId(1000, 0));
    }

    [Fact]
    public void ParseStart_FullId_ParsesBothParts()
    {
        StreamId.ParseStart("1000-5").Should().Be(new StreamId(1000, 5));
    }

    // ── ParseEnd ────────────────────────────────────────────────────────────

    [Fact]
    public void ParseEnd_Plus_ReturnsMaxId()
    {
        StreamId.ParseEnd("+").Should().Be(new StreamId(ulong.MaxValue, ulong.MaxValue));
    }

    [Fact]
    public void ParseEnd_PartialId_SeqIsMaxValue()
    {
        StreamId.ParseEnd("1000").Should().Be(new StreamId(1000, ulong.MaxValue));
    }

    [Fact]
    public void ParseEnd_FullId_ParsesBothParts()
    {
        StreamId.ParseEnd("1000-5").Should().Be(new StreamId(1000, 5));
    }
}
