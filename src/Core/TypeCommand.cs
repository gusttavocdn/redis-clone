namespace codecrafters_redis;

public sealed class TypeCommand(IRedisStore store) : ICommandHandler
{
    public string CommandName => "TYPE";

    public string Handle(string[] args)
    {
        if (args.Length < 2)
            return RespWriter.Error("ERR wrong number of arguments for 'TYPE' command");

        return RespWriter.SimpleString(store.KeyType(args[1]));
    }
}
