namespace codecrafters_redis;

public sealed class PingCommand : ICommandHandler
{
    public string CommandName => "PING";

    public string Handle(string[] args) =>
        args.Length > 1
            ? RespWriter.BulkString(args[1])
            : RespWriter.SimpleString("PONG");
}
