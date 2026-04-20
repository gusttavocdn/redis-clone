namespace codecrafters_redis;

public sealed class EchoCommand : ICommandHandler
{
    public string CommandName => "ECHO";

    public string Handle(string[] args)
    {
        if (args.Length < 2)
            return RespWriter.Error("ERR wrong number of arguments for 'ECHO' command");

        return RespWriter.BulkString(args[1]);
    }
}
