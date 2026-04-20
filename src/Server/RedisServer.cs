using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis;

public sealed class RedisServer : IAsyncDisposable
{
    private readonly TcpListener         _listener;
    private readonly CommandDispatcher   _dispatcher;
    private readonly CancellationTokenSource _cts = new();

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public RedisServer(int port = 0)
    {
        _listener   = new TcpListener(IPAddress.Loopback, port);
        _listener.Start();
        _dispatcher = new CommandDispatcher(new RedisStore());
    }

    public async Task RunAsync()
    {
        try
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync(_cts.Token);
                _ = HandleClientAsync(client);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        using var _ = client;
        var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

        try
        {
            while (true)
            {
                var command = await RespParser.ReadAsync(reader);
                if (command is null) break;

                var response = _dispatcher.Handle(command);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
            }
        }
        catch (IOException) { }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _listener.Stop();
    }
}
