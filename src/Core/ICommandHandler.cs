namespace codecrafters_redis;

public interface ICommandHandler
{
    string CommandName { get; }
    string Handle(string[] args);
}
