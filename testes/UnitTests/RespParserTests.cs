using System.Text;
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

    [Fact]
    public async Task ReadAsync_PingCommand_ReturnsSingleElementArray()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("*1\r\n$4\r\nPING\r\n"));
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var result = await RespParser.ReadAsync(reader);

        result.Should().ContainSingle().Which.Should().Be("PING");
    }

    [Fact]
    public async Task ReadAsync_SetCommand_ReturnsThreeElementArray()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n"));
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var result = await RespParser.ReadAsync(reader);

        result.Should().Equal("SET", "foo", "bar");
    }

    [Fact]
    public async Task ReadAsync_MultipleCommands_ReadsEachCorrectly()
    {
        var bytes = Encoding.UTF8.GetBytes(
            "*1\r\n$4\r\nPING\r\n" +
            "*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n");

        using var stream = new MemoryStream(bytes);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var first  = await RespParser.ReadAsync(reader);
        var second = await RespParser.ReadAsync(reader);

        first.Should().Equal("PING");
        second.Should().Equal("SET", "foo", "bar");
    }

    [Fact]
    public async Task ReadAsync_EndOfStream_ReturnsNull()
    {
        using var stream = new MemoryStream([]);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var result = await RespParser.ReadAsync(reader);

        result.Should().BeNull();
    }
}
