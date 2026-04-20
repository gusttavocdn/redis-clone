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

        _store.Received(1).Set("foo", "bar", null);
    }

    [Fact]
    public void Handle_SetWithoutEnoughArgs_ReturnsError()
    {
        _handler.Handle(["SET", "foo"]).Should().Be("-ERR wrong number of arguments for 'SET' command\r\n");
    }

    [Fact]
    public void Handle_SetWithEX_CallsStoreWithCorrectExpiry()
    {
        _handler.Handle(["SET", "foo", "bar", "EX", "10"]);

        _store.Received(1).Set("foo", "bar", TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Handle_SetWithPX_CallsStoreWithCorrectExpiry()
    {
        _handler.Handle(["SET", "foo", "bar", "PX", "500"]);

        _store.Received(1).Set("foo", "bar", TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Handle_SetWithLowercaseEx_CallsStoreWithCorrectExpiry()
    {
        _handler.Handle(["SET", "foo", "bar", "ex", "10"]);

        _store.Received(1).Set("foo", "bar", TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Handle_SetWithoutExpiry_CallsStoreWithNullExpiry()
    {
        _handler.Handle(["SET", "foo", "bar"]);

        _store.Received(1).Set("foo", "bar", null);
    }

    [Fact]
    public void Handle_SetWithNonIntegerEX_ReturnsError()
    {
        _handler.Handle(["SET", "foo", "bar", "EX", "abc"])
            .Should().Be("-ERR invalid expire time in 'SET' command\r\n");
    }

    [Fact]
    public void Handle_SetWithZeroEX_ReturnsError()
    {
        _handler.Handle(["SET", "foo", "bar", "EX", "0"])
            .Should().Be("-ERR invalid expire time in 'SET' command\r\n");
    }

    [Fact]
    public void Handle_SetWithNegativeEX_ReturnsError()
    {
        _handler.Handle(["SET", "foo", "bar", "EX", "-5"])
            .Should().Be("-ERR invalid expire time in 'SET' command\r\n");
    }

    [Fact]
    public void Handle_SetWithEXButNoValue_ReturnsError()
    {
        _handler.Handle(["SET", "foo", "bar", "EX"])
            .Should().Be("-ERR syntax error\r\n");
    }

    [Fact]
    public void Handle_SetWithUnknownFlag_ReturnsError()
    {
        _handler.Handle(["SET", "foo", "bar", "ZZ", "10"])
            .Should().Be("-ERR syntax error\r\n");
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

    [Fact]
    public void Handle_TypeForExistingKey_ReturnsString()
    {
        _store.KeyType("foo").Returns("string");

        _handler.Handle(["TYPE", "foo"]).Should().Be("+string\r\n");
    }

    [Fact]
    public void Handle_TypeForNonExistentKey_ReturnsNone()
    {
        _store.KeyType("nonexistent").Returns("none");

        _handler.Handle(["TYPE", "nonexistent"]).Should().Be("+none\r\n");
    }

    [Fact]
    public void Handle_TypeWithoutKey_ReturnsError()
    {
        _handler.Handle(["TYPE"]).Should().Be("-ERR wrong number of arguments for 'TYPE' command\r\n");
    }

    [Fact]
    public void Handle_TypeCaseInsensitive_ReturnsString()
    {
        _store.KeyType("foo").Returns("string");

        _handler.Handle(["type", "foo"]).Should().Be("+string\r\n");
    }

    [Fact]
    public void Handle_Type_CallsStoreKeyType()
    {
        _store.KeyType("foo").Returns("string");

        _handler.Handle(["TYPE", "foo"]);

        _store.Received(1).KeyType("foo");
    }
}
