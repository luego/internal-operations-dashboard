namespace InternalOperations.Application;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
