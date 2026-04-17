namespace codecrafters_redis;

public sealed class CommandHandler(IRedisStore store)
{
    public string Handle(string[] command)
    {
        if (command.Length == 0)
            return "-ERR no command provided\r\n";

        return command[0].ToUpperInvariant() switch
        {
            "PING" => HandlePing(command),
            "ECHO" => HandleEcho(command),
            "SET"  => HandleSet(command),
            "GET"  => HandleGet(command),
            var unknown => $"-ERR unknown command '{unknown}'\r\n"
        };
    }

    private static string HandlePing(string[] command)
    {
        if (command.Length > 1)
        {
            var message = command[1];
            return $"${message.Length}\r\n{message}\r\n";
        }

        return "+PONG\r\n";
    }

    private static string HandleEcho(string[] command)
    {
        if (command.Length < 2)
            return "-ERR wrong number of arguments for 'ECHO' command\r\n";

        var message = command[1];
        return $"${message.Length}\r\n{message}\r\n";
    }

    private string HandleSet(string[] command)
    {
        if (command.Length < 3)
            return "-ERR wrong number of arguments for 'SET' command\r\n";

        store.Set(command[1], command[2]);
        return "+OK\r\n";
    }

    private string HandleGet(string[] command)
    {
        if (command.Length < 2)
            return "-ERR wrong number of arguments for 'GET' command\r\n";

        var value = store.Get(command[1]);
        return value is null
            ? "$-1\r\n"
            : $"${value.Length}\r\n{value}\r\n";
    }
}
