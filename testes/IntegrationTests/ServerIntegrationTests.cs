namespace codecrafters_redis.IntegrationTests;

public class ServerIntegrationTests
{
    // End-to-end tests connecting via TCP to a running server instance will live here.
    // Example structure:
    //
    // [Fact]
    // public async Task Server_RespondsToPing_WithPong()
    // {
    //     using var client = new TcpClient("127.0.0.1", 6379);
    //     using var stream = client.GetStream();
    //     var ping = Encoding.UTF8.GetBytes("*1\r\n$4\r\nPING\r\n");
    //     await stream.WriteAsync(ping);
    //     var buffer = new byte[7];
    //     await stream.ReadAsync(buffer);
    //     Assert.Equal("+PONG\r\n", Encoding.UTF8.GetString(buffer));
    // }
}
