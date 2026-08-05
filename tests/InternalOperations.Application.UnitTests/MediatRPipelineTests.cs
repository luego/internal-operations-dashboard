using MediatR;

namespace InternalOperations.Application.UnitTests;

public sealed class MediatRPipelineTests
{
    [Fact]
    public async Task ValidationBehaviorInvokesNextHandlerAndReturnsResult()
    {
        var behavior = new ValidationBehavior<TestRequest, Result<string>>([
            new TestRequestValidator(),
        ]);

        var response = await behavior.Handle(
            new TestRequest("ok"),
            ct => Task.FromResult(Result<string>.Success("ok")),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal("ok", response.Value);
    }

    private sealed record TestRequest(string Value) : IRequest<Result<string>>;

    private sealed class TestRequestValidator : IRequestValidator<TestRequest>
    {
        public Result Validate(TestRequest request)
        {
            return string.IsNullOrWhiteSpace(request.Value)
                ? Result.Failure(Error.Validation("VALUE_REQUIRED", "Value is required."))
                : Result.Success();
        }
    }
}
