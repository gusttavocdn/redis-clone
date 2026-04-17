namespace codecrafters_redis;

public static class RespParser
{
    public static string[] Parse(string input)
    {
        if (!input.StartsWith('*'))
            throw new InvalidDataException($"Expected '*' but got '{input[0]}'");

        var lines = input.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        // After skipping lines[0] ("*N"), the remaining lines alternate between
        // "$<len>" (even index) and the actual value (odd index). We keep only odd ones.
        return lines.Skip(1)
                    .Where((_, i) => i % 2 == 1)
                    .ToArray();
    }
}