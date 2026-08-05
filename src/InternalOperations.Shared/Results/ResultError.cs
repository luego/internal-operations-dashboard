namespace InternalOperations.Shared.Results;

public sealed record ResultError(
    string Code,
    string Description,
    ErrorType Type);
