using Building_Blocks.Extensions;
using Building_Blocks.SharedResponse;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Building_Blocks.Exceptions;

public class CustomExceptionHandler(ILogger<CustomExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        IResult response;

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .Select(failure => Error.Validation(
                    string.IsNullOrWhiteSpace(failure.PropertyName) ? "Validation" : failure.PropertyName,
                    failure.ErrorMessage))
                .DefaultIfEmpty(Error.Validation("Validation", "The request is invalid."));

            response = Result.Fail(errors).ToHttpResult();
        }
        else if (exception is BadHttpRequestException badRequestException)
        {
            response = Results.Problem(
                title: "Request.Invalid",
                detail: "The request could not be processed.",
                statusCode: badRequestException.StatusCode);
        }
        else
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}. Trace ID: {TraceId}",
                httpContext.Request.Method, httpContext.Request.Path, httpContext.TraceIdentifier);

            response = Results.Problem(
                title: "Server.Error",
                detail: "An unexpected error occurred.",
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?> { ["traceId"] = httpContext.TraceIdentifier });
        }

        await response.ExecuteAsync(httpContext);
        return true;
    }
}
