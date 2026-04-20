using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace codecrafters_redis.UnitTests;

public class StreamCommandTests
{
    private readonly IRedisStore _store = Substitute.For<IRedisStore>();
    private readonly CommandHandler _handler;

    public StreamCommandTests()
    {
        _handler = new CommandHandler(_store);
    }

    public static TheoryData<string[]> MissingArgCommands =>
    [
        new[] { "XADD" },
        new[] { "XADD", "key" },
        new[] { "XADD", "key", "*" },
        new[] { "XADD", "key", "*", "field" },
    ];

    [Theory]
    [MemberData(nameof(MissingArgCommands))]
    public void Handle_XAdd_MissingArgs_ReturnsError(string[] command)
    {
        _handler.Handle(command).Should().Be("-ERR wrong number of arguments for 'XADD' command\r\n");
    }

    [Fact]
    public void Handle_XAdd_OddFieldCount_ReturnsError()
    {
        _handler.Handle(["XADD", "key", "*", "f1", "v1", "f2"])
                .Should().Be("-ERR wrong number of arguments for 'XADD' command\r\n");
    }

    [Fact]
    public void Handle_XAdd_CallsStoreWithCorrectArgs()
    {
        _store.XAdd(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string)>>())
              .Returns("1000-0");

        _handler.Handle(["XADD", "mystream", "*", "name", "John"]);

        _store.Received(1).XAdd(
            "mystream",
            "*",
            Arg.Is<IReadOnlyList<(string Field, string Value)>>(l =>
                l.Count == 1 && l[0].Field == "name" && l[0].Value == "John"));
    }

    [Fact]
    public void Handle_XAdd_ReturnsGeneratedId()
    {
        _store.XAdd(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string)>>())
              .Returns("1526919030474-0");

        var response = _handler.Handle(["XADD", "mystream", "*", "name", "John"]);

        response.Should().Be("$15\r\n1526919030474-0\r\n");
    }

    [Fact]
    public void Handle_XAdd_StoreThrows_ReturnsErrorResponse()
    {
        _store.XAdd(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string)>>())
              .Throws(new InvalidOperationException(
                  "ERR The ID specified in XADD is equal or smaller than the target stream top item"));

        var response = _handler.Handle(["XADD", "mystream", "0-0", "f", "v"]);

        response.Should().Be(
            "-ERR The ID specified in XADD is equal or smaller than the target stream top item\r\n");
    }

    [Fact]
    public void Handle_XAdd_CaseInsensitive()
    {
        _store.XAdd(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string)>>())
              .Returns("1000-0");

        var response = _handler.Handle(["xadd", "mystream", "*", "f", "v"]);

        response.Should().NotStartWith("-ERR unknown command");
    }

    [Fact]
    public void Handle_Type_StreamKey_ReturnsStream()
    {
        _store.KeyType("mystream").Returns("stream");

        _handler.Handle(["TYPE", "mystream"]).Should().Be("+stream\r\n");
    }

    [Fact]
    public void Handle_XAdd_MultipleFields_CallsStoreCorrectly()
    {
        _store.XAdd(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string)>>())
              .Returns("1000-0");

        _handler.Handle(["XADD", "s", "*", "f1", "v1", "f2", "v2"]);

        _store.Received(1).XAdd(
            "s",
            "*",
            Arg.Is<IReadOnlyList<(string Field, string Value)>>(l =>
                l.Count == 2 &&
                l[0].Field == "f1" && l[0].Value == "v1" &&
                l[1].Field == "f2" && l[1].Value == "v2"));
    }
}
