using Building_Blocks.SharedResponse;
using Microsoft.AspNetCore.Http;

namespace Building_Blocks.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
        => result.IsSuccess ? Results.NoContent() : ToProblem(result);

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsFailure)
        {
            return ToProblem(result);
        }

        var value = result.GetValueOrThrow();
        return onSuccess is null ? Results.Ok(value) : onSuccess(value);
    }

    private static IResult ToProblem(Result result)
    {
        if (result.Errors.All(error => error.Type == ErrorType.Validation))
        {
            var errors = result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());

            return Results.ValidationProblem(errors, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var error = result.Errors[0];

        return Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: GetStatusCode(error.Type),
            extensions: new Dictionary<string, object?> { ["errors"] = result.Errors });
    }

    private static int GetStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Unauthorized or ErrorType.InvalidCredentials => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.BadRequest => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}
