using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;

var server = new TcpListener(IPAddress.Any, 6379);
server.Start();

Console.WriteLine("Redis server listening on port 6379...");

while (true)
{
    var socket = await server.AcceptSocketAsync();
    _ = HandleClientAsync(socket);
}

static async Task HandleClientAsync(Socket socket)
{
    var handler = new CommandHandler();
    var buffer = new byte[1024];    

    try
    {
        while (socket.Connected)
        {
            int bytesRead = await socket.ReceiveAsync(buffer);
            if (bytesRead == 0) break;

            var input = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            var command = RespParser.Parse(input);
            var response = handler.Handle(command);

            await socket.SendAsync(Encoding.UTF8.GetBytes(response));
        }
    }
    finally
    {
        socket.Close();
    }
}
