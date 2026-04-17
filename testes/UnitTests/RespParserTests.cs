using FluentAssertions;

namespace codecrafters_redis.UnitTests;

public class RespParserTests
{
    [Fact]
    public void Parse_PingCommand_ReturnsSingleElementArray()
    {
        var result = RespParser.Parse("*1\r\n$4\r\nPING\r\n");

        result.Should().ContainSingle().Which.Should().Be("PING");
    }

    [Fact]
    public void Parse_PingWithMessage_ReturnsTwoElementArray()
    {
        var result = RespParser.Parse("*2\r\n$4\r\nPING\r\n$5\r\nhello\r\n");

        result.Should().Equal("PING", "hello");
    }

    [Fact]
    public void Parse_CommandIsCasePreserved()
    {
        var result = RespParser.Parse("*1\r\n$4\r\nping\r\n");

        result.Should().ContainSingle().Which.Should().Be("ping");
    }

    [Fact]
    public void Parse_EmptyArray_ReturnsEmptyArray()
    {
        var result = RespParser.Parse("*0\r\n");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_ThrowsOnInvalidPrefix()
    {
        var act = () => RespParser.Parse("PING\r\n");

        act.Should().Throw<InvalidDataException>();
    }
}
