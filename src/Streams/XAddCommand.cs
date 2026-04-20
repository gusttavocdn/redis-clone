namespace codecrafters_redis;

public sealed class XAddCommand(IRedisStore store) : ICommandHandler
{
    public string CommandName => "XADD";

    public string Handle(string[] args)
    {
        if (args.Length < 5 || (args.Length - 3) % 2 != 0)
            return RespWriter.Error("ERR wrong number of arguments for 'XADD' command");

        var key         = args[1];
        var requestedId = args[2];

        var fields = new List<(string Field, string Value)>();
        for (var i = 3; i < args.Length; i += 2)
            fields.Add((args[i], args[i + 1]));

        var result = store.XAdd(key, requestedId, fields);
        return result.IsError
            ? RespWriter.Error(result.Error!)
            : RespWriter.BulkString(result.Value);
    }
}
