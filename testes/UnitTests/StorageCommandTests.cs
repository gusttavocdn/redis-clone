using FluentAssertions;
using NSubstitute;

namespace codecrafters_redis.UnitTests;

public class StorageCommandTests
{
    private readonly IRedisStore _store = Substitute.For<IRedisStore>();
    private readonly CommandHandler _handler;

    public StorageCommandTests()
    {
        _handler = new CommandHandler(_store);
    }

    [Fact]
    public void Handle_Set_ReturnsOk()
    {
        _handler.Handle(["SET", "foo", "bar"]).Should().Be("+OK\r\n");
    }

    [Fact]
    public void Handle_SetCaseInsensitive_ReturnsOk()
    {
        _handler.Handle(["set", "foo", "bar"]).Should().Be("+OK\r\n");
    }

    [Fact]
    public void Handle_Set_CallsStoreWithCorrectArguments()
    {
        _handler.Handle(["SET", "foo", "bar"]);

        _store.Received(1).Set("foo", "bar");
    }

    [Fact]
    public void Handle_SetWithoutEnoughArgs_ReturnsError()
    {
        _handler.Handle(["SET", "foo"]).Should().Be("-ERR wrong number of arguments for 'SET' command\r\n");
    }

    [Fact]
    public void Handle_GetExistingKey_ReturnsValue()
    {
        _store.Get("foo").Returns("bar");

        _handler.Handle(["GET", "foo"]).Should().Be("$3\r\nbar\r\n");
    }

    [Fact]
    public void Handle_GetNonExistentKey_ReturnsNullBulkString()
    {
        _store.Get("nonexistent").Returns((string?)null);

        _handler.Handle(["GET", "nonexistent"]).Should().Be("$-1\r\n");
    }

    [Fact]
    public void Handle_GetWithoutKey_ReturnsError()
    {
        _handler.Handle(["GET"]).Should().Be("-ERR wrong number of arguments for 'GET' command\r\n");
    }
}
