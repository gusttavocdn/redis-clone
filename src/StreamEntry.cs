namespace codecrafters_redis;

public sealed record StreamEntry(
    string Id,
    IReadOnlyList<(string Field, string Value)> Fields);
