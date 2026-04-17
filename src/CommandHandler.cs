namespace codecrafters_redis;

public class CommandHandler
{
    public string Handle(string[] command)
    {
        if (command.Length == 0)
            return "-ERR no command provided\r\n";

        return command[0].ToUpperInvariant() switch
        {
            "PING" => HandlePing(command),
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
}