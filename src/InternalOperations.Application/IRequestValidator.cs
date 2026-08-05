using MediatR;

namespace InternalOperations.Application;

public interface IRequestValidator<TRequest>
{
    Result Validate(TRequest request);
}

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IReadOnlyList<IRequestValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IRequestValidator<TRequest>> validators)
    {
        _validators = validators.ToList();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        foreach (var validator in _validators)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsSuccess)
            {
                return CreateFailureResponse(validationResult.Errors);
            }
        }

        return await next(cancellationToken);
    }

    private static TResponse CreateFailureResponse(IReadOnlyList<Error> errors)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(errors.ToArray());
        }

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result<>).MakeGenericType(valueType).GetMethod(nameof(Result<object>.Failure), [typeof(Error[])])!;
            return (TResponse)failureMethod.Invoke(null, [errors.ToArray()])!;
        }

        throw new InvalidOperationException($"Validation behavior supports only Result and Result<T> responses. Actual type: {typeof(TResponse).FullName}.");
    }
}
