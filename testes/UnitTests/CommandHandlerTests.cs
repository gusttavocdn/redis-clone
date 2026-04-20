using FluentAssertions;
using NSubstitute;

namespace codecrafters_redis.UnitTests;

public class CommandHandlerTests
{
    private readonly CommandDispatcher _handler = new(Substitute.For<IRedisStore>());

    [Fact]
    public void Handle_Ping_ReturnsPong()
    {
        _handler.Handle(["PING"]).Should().Be("+PONG\r\n");
    }

    [Fact]
    public void Handle_PingCaseInsensitive_ReturnsPong()
    {
        _handler.Handle(["ping"]).Should().Be("+PONG\r\n");
    }

    [Fact]
    public void Handle_PingWithMessage_ReturnsBulkStringMessage()
    {
        _handler.Handle(["PING", "hello"]).Should().Be("$5\r\nhello\r\n");
    }

    [Fact]
    public void Handle_PingWithEmptyMessage_ReturnsBulkStringEmpty()
    {
        _handler.Handle(["PING", ""]).Should().Be("$0\r\n\r\n");
    }

    [Fact]
    public void Handle_UnknownCommand_ReturnsErrorResponse()
    {
        _handler.Handle(["UNKNOWN"]).Should().Be("-ERR unknown command 'UNKNOWN'\r\n");
    }

    [Fact]
    public void Handle_Echo_ReturnsBulkStringMessage()
    {
        _handler.Handle(["ECHO", "hello"]).Should().Be("$5\r\nhello\r\n");
    }

    [Fact]
    public void Handle_EchoCaseInsensitive_ReturnsBulkStringMessage()
    {
        _handler.Handle(["echo", "hello"]).Should().Be("$5\r\nhello\r\n");
    }

    [Fact]
    public void Handle_EchoWithoutArgument_ReturnsError()
    {
        _handler.Handle(["ECHO"]).Should().Be("-ERR wrong number of arguments for 'ECHO' command\r\n");
    }
}
