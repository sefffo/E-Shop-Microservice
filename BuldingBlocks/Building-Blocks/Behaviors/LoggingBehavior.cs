using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Building_Blocks.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);
        var start = Stopwatch.GetTimestamp();

        try
        {
            return await next(cancellationToken);
        }
        finally
        {
            logger.LogInformation("Finished handling {RequestName} in {ElapsedMilliseconds} ms",
                requestName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }
}
