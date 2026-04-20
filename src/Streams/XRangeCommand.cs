using System.Text;

namespace codecrafters_redis;

public sealed class XRangeCommand(IRedisStore store) : ICommandHandler
{
    public string CommandName => "XRANGE";

    public string Handle(string[] args)
    {
        if (args.Length < 4)
            return RespWriter.Error("ERR wrong number of arguments for 'xrange' command");

        var entries = store.XRange(args[1], args[2], args[3]);

        if (entries.Count == 0)
            return RespWriter.EmptyArray();

        var sb = new StringBuilder();
        sb.Append(RespWriter.Array(entries.Count));

        foreach (var entry in entries)
        {
            sb.Append(RespWriter.Array(2));
            sb.Append(RespWriter.BulkString(entry.Id));
            sb.Append(RespWriter.Array(entry.Fields.Count * 2));
            foreach (var (field, value) in entry.Fields)
            {
                sb.Append(RespWriter.BulkString(field));
                sb.Append(RespWriter.BulkString(value));
            }
        }

        return sb.ToString();
    }
}
