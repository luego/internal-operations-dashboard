namespace InternalOperations.Shared.Results;

public class Result
{
    protected Result(bool isSuccess, ResultError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ResultError? Error { get; }

    public static Result Success()
        => new(true, null);

    public static Result Failure(ResultError error)
        => new(false, error);

    public static Result<T> Success<T>(T value)
        => new Result<T>(value);

    public static Result<T> Failure<T>(ResultError error)
        => new Result<T>(error);
}

public sealed class Result<T> : Result
{
    internal Result(T value)
        : base(true, null)
    {
        Value = value;
    }

    internal Result(ResultError error)
        : base(false, error)
    {
    }

    public T? Value { get; }
}