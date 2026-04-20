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
            "TYPE" => HandleType(command),
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

        var key = command[1];
        var value = command[2];
        TimeSpan? expiry = null;

        var i = 3;
        while (i < command.Length)
        {
            var flag = command[i].ToUpperInvariant();
            if (flag is "EX" or "PX")
            {
                if (i + 1 >= command.Length)
                    return "-ERR syntax error\r\n";

                if (!long.TryParse(command[i + 1], out var amount) || amount <= 0)
                    return "-ERR invalid expire time in 'SET' command\r\n";

                expiry = flag == "EX"
                    ? TimeSpan.FromSeconds(amount)
                    : TimeSpan.FromMilliseconds(amount);

                i += 2;
            }
            else
            {
                return "-ERR syntax error\r\n";
            }
        }

        store.Set(key, value, expiry);
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

    private string HandleType(string[] command)
    {
        if (command.Length < 2)
            return "-ERR wrong number of arguments for 'TYPE' command\r\n";

        return $"+{store.KeyType(command[1])}\r\n";
    }
}
