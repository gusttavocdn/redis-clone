namespace codecrafters_redis;

public readonly record struct StreamId(ulong Ms, ulong Seq)
{
    public static StreamId Parse(string id)
    {
        var dash = id.IndexOf('-');
        return new(ulong.Parse(id[..dash]), ulong.Parse(id[(dash + 1)..]));
    }

    public bool IsGreaterThan(StreamId other) =>
        Ms > other.Ms || (Ms == other.Ms && Seq > other.Seq);

    public override string ToString() => $"{Ms}-{Seq}";
}
