using InternalOperations.Application;

namespace InternalOperations.Application.UnitTests;

public sealed class ResultTests
{
    [Fact]
    public void SuccessResultHasNoErrors()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValueResultReturnsValueWhenSuccessful()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void FailureResultContainsErrorDetails()
    {
        var result = Result.Failure(
            Error.Validation("ITEM_REQUIRED", "A value is required."));

        Assert.False(result.IsSuccess);
        Assert.Single(result.Errors);
        Assert.Equal("ITEM_REQUIRED", result.Errors[0].Code);
        Assert.Equal(ErrorType.Validation, result.Errors[0].Type);
    }
}
