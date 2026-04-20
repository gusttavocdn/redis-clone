using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using codecrafters_redis;

namespace codecrafters_redis.IntegrationTests;

public class ServerIntegrationTests
{
    // Reads one complete RESP response from the StreamReader.
    private static async Task<string> ReadResponseAsync(StreamReader reader)
    {
        var first = await reader.ReadLineAsync() ?? "";

        return first[0] switch
        {
            '+' or '-' or ':' => first + "\r\n",
            '$' when first == "$-1" => "$-1\r\n",
            '$' => await ReadBulkAsync(reader, first),
            _ => first + "\r\n"
        };
    }

    private static async Task<string> ReadBulkAsync(StreamReader reader, string lenLine)
    {
        var len = int.Parse(lenLine[1..]);
        var buf = new char[len];
        await reader.ReadBlockAsync(buf, 0, len);
        await reader.ReadLineAsync(); // consume trailing \r\n
        return $"{lenLine}\r\n{new string(buf)}\r\n";
    }

    [Fact]
    public async Task Ping_ReturnsPong()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await stream.WriteAsync("*1\r\n$4\r\nPING\r\n"u8.ToArray());

        (await ReadResponseAsync(reader)).Should().Be("+PONG\r\n");
    }

    [Fact]
    public async Task Echo_ReturnsBulkString()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await stream.WriteAsync("*2\r\n$4\r\nECHO\r\n$5\r\nhello\r\n"u8.ToArray());

        (await ReadResponseAsync(reader)).Should().Be("$5\r\nhello\r\n");
    }

    [Fact]
    public async Task SetAndGet_RoundTrip()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await stream.WriteAsync("*3\r\n$3\r\nSET\r\n$3\r\nfoo\r\n$3\r\nbar\r\n"u8.ToArray());
        (await ReadResponseAsync(reader)).Should().Be("+OK\r\n");

        await stream.WriteAsync("*2\r\n$3\r\nGET\r\n$3\r\nfoo\r\n"u8.ToArray());
        (await ReadResponseAsync(reader)).Should().Be("$3\r\nbar\r\n");
    }

    [Fact]
    public async Task Get_MissingKey_ReturnsNullBulk()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await stream.WriteAsync("*2\r\n$3\r\nGET\r\n$7\r\nmissing\r\n"u8.ToArray());

        (await ReadResponseAsync(reader)).Should().Be("$-1\r\n");
    }

    [Fact]
    public async Task XAdd_ReturnsValidStreamId()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await stream.WriteAsync("*5\r\n$4\r\nXADD\r\n$8\r\nmystream\r\n$1\r\n*\r\n$4\r\nname\r\n$4\r\nJohn\r\n"u8.ToArray());
        var response = await ReadResponseAsync(reader);

        // Response is a bulk string containing a valid "ms-seq" stream ID
        response.Should().StartWith("$");
        var id = response.Split("\r\n")[1];
        id.Should().MatchRegex(@"^\d+-\d+$");
    }

    [Fact]
    public async Task Type_AfterXAdd_ReturnsStream()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await stream.WriteAsync("*5\r\n$4\r\nXADD\r\n$8\r\nmystream\r\n$1\r\n*\r\n$4\r\nname\r\n$4\r\nJohn\r\n"u8.ToArray());
        await ReadResponseAsync(reader); // discard XADD response

        await stream.WriteAsync("*2\r\n$4\r\nTYPE\r\n$8\r\nmystream\r\n"u8.ToArray());
        (await ReadResponseAsync(reader)).Should().Be("+stream\r\n");
    }

    [Fact]
    public async Task UnknownCommand_ReturnsError()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        await stream.WriteAsync("*1\r\n$7\r\nUNKNOWN\r\n"u8.ToArray());

        (await ReadResponseAsync(reader)).Should().StartWith("-ERR unknown command");
    }

    [Fact]
    public async Task MultipleCommandsOnSameConnection_WorkCorrectly()
    {
        await using var server = new RedisServer();
        _ = server.RunAsync();

        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", server.Port);
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        // Three sequential commands on the same TCP connection
        await stream.WriteAsync("*1\r\n$4\r\nPING\r\n"u8.ToArray());
        (await ReadResponseAsync(reader)).Should().Be("+PONG\r\n");

        await stream.WriteAsync("*3\r\n$3\r\nSET\r\n$1\r\nk\r\n$1\r\nv\r\n"u8.ToArray());
        (await ReadResponseAsync(reader)).Should().Be("+OK\r\n");

        await stream.WriteAsync("*2\r\n$3\r\nGET\r\n$1\r\nk\r\n"u8.ToArray());
        (await ReadResponseAsync(reader)).Should().Be("$1\r\nv\r\n");
    }
}
