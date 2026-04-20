using FluentAssertions;
using NSubstitute;

namespace codecrafters_redis.UnitTests;

public class StreamCommandTests
{
    private readonly IRedisStore _store = Substitute.For<IRedisStore>();
    private readonly CommandDispatcher _handler;

    public StreamCommandTests()
    {
        _handler = new CommandDispatcher(_store);
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
              .Returns(Result<string>.Ok("1000-0"));

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
              .Returns(Result<string>.Ok("1526919030474-0"));

        var response = _handler.Handle(["XADD", "mystream", "*", "name", "John"]);

        response.Should().Be("$15\r\n1526919030474-0\r\n");
    }

    [Fact]
    public void Handle_XAdd_StoreReturnsError_ReturnsErrorResponse()
    {
        _store.XAdd(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string)>>())
              .Returns(Result<string>.Fail(
                  "ERR The ID specified in XADD is equal or smaller than the target stream top item"));

        var response = _handler.Handle(["XADD", "mystream", "0-0", "f", "v"]);

        response.Should().Be(
            "-ERR The ID specified in XADD is equal or smaller than the target stream top item\r\n");
    }

    [Fact]
    public void Handle_XAdd_CaseInsensitive()
    {
        _store.XAdd(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<(string, string)>>())
              .Returns(Result<string>.Ok("1000-0"));

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
              .Returns(Result<string>.Ok("1000-0"));

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

public class XRangeCommandTests
{
    private readonly IRedisStore _store = Substitute.For<IRedisStore>();
    private readonly CommandDispatcher _handler;

    public XRangeCommandTests()
    {
        _handler = new CommandDispatcher(_store);
        _store.XRange(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
              .Returns([]);
    }

    public static TheoryData<string[]> TooFewArgCommands =>
    [
        new[] { "XRANGE" },
        new[] { "XRANGE", "key" },
        new[] { "XRANGE", "key", "-" },
    ];

    [Theory]
    [MemberData(nameof(TooFewArgCommands))]
    public void Handle_TooFewArgs_ReturnsError(string[] command)
    {
        _handler.Handle(command)
                .Should().Be("-ERR wrong number of arguments for 'xrange' command\r\n");
    }

    [Fact]
    public void Handle_EmptyResult_ReturnsEmptyArray()
    {
        _store.XRange("s", "-", "+").Returns([]);

        _handler.Handle(["XRANGE", "s", "-", "+"]).Should().Be("*0\r\n");
    }

    [Fact]
    public void Handle_SingleEntryOneField_FormatsCorrectly()
    {
        _store.XRange("s", "-", "+").Returns(
        [
            new StreamEntry("1-1", [("f", "v")])
        ]);

        var response = _handler.Handle(["XRANGE", "s", "-", "+"]);

        response.Should().Be("*1\r\n*2\r\n$3\r\n1-1\r\n*2\r\n$1\r\nf\r\n$1\r\nv\r\n");
    }

    [Fact]
    public void Handle_SingleEntryTwoFields_FieldsArrayHasFourElements()
    {
        _store.XRange("s", "-", "+").Returns(
        [
            new StreamEntry("1-1", [("name", "John"), ("age", "30")])
        ]);

        var response = _handler.Handle(["XRANGE", "s", "-", "+"]);

        // outer *1, entry *2, id, fields *4 (2 pairs)
        response.Should().StartWith("*1\r\n*2\r\n");
        response.Should().Contain("*4\r\n");
        response.Should().Contain("$4\r\nname\r\n");
        response.Should().Contain("$4\r\nJohn\r\n");
        response.Should().Contain("$3\r\nage\r\n");
        response.Should().Contain("$2\r\n30\r\n");
    }

    [Fact]
    public void Handle_TwoEntries_OuterArrayHasCountTwo()
    {
        _store.XRange("s", "-", "+").Returns(
        [
            new StreamEntry("1-1", [("f", "v")]),
            new StreamEntry("2-1", [("f", "v")])
        ]);

        _handler.Handle(["XRANGE", "s", "-", "+"]).Should().StartWith("*2\r\n");
    }

    [Fact]
    public void Handle_PassesKeyAndBoundariesToStore()
    {
        _handler.Handle(["XRANGE", "mykey", "5-0", "10-3"]);

        _store.Received(1).XRange("mykey", "5-0", "10-3");
    }

    [Fact]
    public void Handle_CaseInsensitive_Dispatches()
    {
        var response = _handler.Handle(["xrange", "s", "-", "+"]);

        response.Should().NotStartWith("-ERR unknown command");
    }
}
