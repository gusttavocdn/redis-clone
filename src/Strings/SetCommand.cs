namespace codecrafters_redis;

public sealed class SetCommand(IRedisStore store) : ICommandHandler
{
    public string CommandName => "SET";

    public string Handle(string[] args)
    {
        if (args.Length < 3)
            return RespWriter.Error("ERR wrong number of arguments for 'SET' command");

        var key   = args[1];
        var value = args[2];
        TimeSpan? expiry = null;

        var i = 3;
        while (i < args.Length)
        {
            var flag = args[i].ToUpperInvariant();
            if (flag is "EX" or "PX")
            {
                if (i + 1 >= args.Length)
                    return RespWriter.Error("ERR syntax error");

                if (!long.TryParse(args[i + 1], out var amount) || amount <= 0)
                    return RespWriter.Error("ERR invalid expire time in 'SET' command");

                expiry = flag == "EX"
                    ? TimeSpan.FromSeconds(amount)
                    : TimeSpan.FromMilliseconds(amount);

                i += 2;
            }
            else
            {
                return RespWriter.Error("ERR syntax error");
            }
        }

        store.Set(key, value, expiry);
        return RespWriter.SimpleString("OK");
    }
}
