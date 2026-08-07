
using InternalOperations.Application;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.ErrorHandling;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(
        this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        var status = result.Error?.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };
        var problem = new ProblemDetails { Status = status, Title = result.Error?.Message };
        problem.Extensions["code"] = result.Error?.Code;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
