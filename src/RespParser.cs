namespace codecrafters_redis;

public static class RespParser
{
    public static string[] Parse(string input)
    {
        if (!input.StartsWith('*'))
            throw new InvalidDataException($"Expected '*' but got '{input[0]}'");

        var lines = input.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        int count = int.Parse(lines[0][1..]);

        var result = new string[count];
        int lineIndex = 1;

        for (int i = 0; i < count; i++)
        {
            // skip the $<length> line
            lineIndex++;
            result[i] = lines[lineIndex++];
        }

        return result;
    }
}