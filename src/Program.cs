using codecrafters_redis;

await using var server = new RedisServer(6379);
Console.WriteLine("Redis server listening on port 6379...");
await server.RunAsync();
