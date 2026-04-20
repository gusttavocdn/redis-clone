namespace codecrafters_redis;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, ICommandHandler> _handlers;

    public CommandDispatcher(IRedisStore store)
    {
        ICommandHandler[] commands =
        [
            new PingCommand(),
            new EchoCommand(),
            new SetCommand(store),
            new GetCommand(store),
            new TypeCommand(store),
            new XAddCommand(store),
            new XRangeCommand(store),
        ];

        _handlers = commands.ToDictionary(h => h.CommandName, StringComparer.OrdinalIgnoreCase);
    }

    public string Handle(string[] command)
    {
        if (command.Length == 0)
            return RespWriter.Error("ERR no command provided");

        return _handlers.TryGetValue(command[0], out var handler)
            ? handler.Handle(command)
            : RespWriter.Error($"ERR unknown command '{command[0].ToUpperInvariant()}'");
    }
}
