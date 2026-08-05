namespace InternalOperations.Application;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
}
