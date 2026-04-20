namespace codecrafters_redis;

public sealed class GetCommand(IRedisStore store) : ICommandHandler
{
    public string CommandName => "GET";

    public string Handle(string[] args)
    {
        if (args.Length < 2)
            return RespWriter.Error("ERR wrong number of arguments for 'GET' command");

        var value = store.Get(args[1]);
        return value is null ? RespWriter.NullBulk() : RespWriter.BulkString(value);
    }
}
