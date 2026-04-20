namespace codecrafters_redis;

public static class RespParser
{
    // Kept for unit tests and simple parsing scenarios.
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

    // Reads one complete RESP command from a persistent StreamReader.
    // Returns null when the client closes the connection.
    public static async Task<string[]?> ReadAsync(StreamReader reader)
    {
        var firstLine = await reader.ReadLineAsync();
        if (firstLine is null) return null;

        if (!firstLine.StartsWith('*'))
            throw new InvalidDataException($"Expected '*' but got '{firstLine[0]}'");

        var count = int.Parse(firstLine[1..]);
        var args  = new string[count];

        for (var i = 0; i < count; i++)
        {
            var lenLine = await reader.ReadLineAsync()
                          ?? throw new InvalidDataException("Unexpected end of stream");

            if (!lenLine.StartsWith('$'))
                throw new InvalidDataException($"Expected '$' but got '{lenLine[0]}'");

            var len = int.Parse(lenLine[1..]);
            var buf = new char[len];
            await reader.ReadBlockAsync(buf, 0, len);
            await reader.ReadLineAsync(); // consume trailing \r\n

            args[i] = new string(buf);
        }

        return args;
    }
}
