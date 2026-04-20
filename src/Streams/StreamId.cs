namespace codecrafters_redis;

public readonly record struct StreamId(ulong Ms, ulong Seq)
{
    public static StreamId Parse(string id)
    {
        var dash = id.IndexOf('-');
        return new StreamId(ulong.Parse(id[..dash]), ulong.Parse(id[(dash + 1)..]));
    }

    public static StreamId ParseStart(string id) => id switch
    {
        "-"                      => new StreamId(0, 0),
        _ when !id.Contains('-') => new StreamId(ulong.Parse(id), 0),
        _                        => Parse(id)
    };

    public static StreamId ParseEnd(string id) => id switch
    {
        "+"                      => new StreamId(ulong.MaxValue, ulong.MaxValue),
        _ when !id.Contains('-') => new StreamId(ulong.Parse(id), ulong.MaxValue),
        _                        => Parse(id)
    };

    public bool IsGreaterThan(StreamId other) =>
        Ms > other.Ms || (Ms == other.Ms && Seq > other.Seq);

    public override string ToString() => $"{Ms}-{Seq}";
}
