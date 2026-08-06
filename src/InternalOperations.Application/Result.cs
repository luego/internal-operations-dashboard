using System.Diagnostics.CodeAnalysis;

namespace InternalOperations.Application;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    Failure,
}

[SuppressMessage("Design", "CA1716:IdentifiersShouldNotMatchKeywords", Justification = "The domain contract deliberately uses Error as the stable application result type.")]
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}

public abstract class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        Error = errors.Count > 0 ? errors[0] : null;
    }

    public bool IsSuccess { get; }

    public Error? Error { get; }

    public IReadOnlyList<Error> Errors { get; }

    public static Result Success() => new SuccessResult();

    public static Result Failure(params Error[] errors) => new FailedResult(errors);

    private sealed class SuccessResult : Result
    {
        public SuccessResult() : base(true, Array.Empty<Error>())
        {
        }
    }

    private sealed class FailedResult : Result
    {
        public FailedResult(IReadOnlyList<Error> errors) : base(false, errors)
        {
        }
    }
}

[SuppressMessage("Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Justification = "This generic result type is the public factory for typed results and must expose the same API shape expected by the application layer.")]
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<Error>());

    public static new Result<T> Failure(params Error[] errors) => new(false, default, errors);
}
