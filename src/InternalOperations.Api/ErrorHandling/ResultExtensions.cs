
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

        return result.Error?.Type switch
        {
            ErrorType.Validation =>
                new BadRequestObjectResult(result.Error),

            ErrorType.NotFound =>
                new NotFoundObjectResult(result.Error),

            ErrorType.Conflict =>
                new ConflictObjectResult(result.Error),

            ErrorType.Unauthorized =>
                new UnauthorizedObjectResult(result.Error),

            ErrorType.Forbidden =>
                new ObjectResult(result.Error)
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

            _ => new ObjectResult(result.Error)
            {
                StatusCode =
                    StatusCodes.Status500InternalServerError
            }
        };
    }
}
