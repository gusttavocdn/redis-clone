namespace codecrafters_redis;

public readonly record struct Result<T>
{
    private readonly T? _value;

    private Result(T? value, string? error)
    {
        _value = value;
        Error  = error;
    }

    public string? Error { get; }
    public bool    IsError => Error is not null;
    public T       Value   => !IsError ? _value! : throw new InvalidOperationException($"Result is an error: {Error}");

    public static Result<T> Ok(T value)        => new(value, null);
    public static Result<T> Fail(string error) => new(default, error);
}
