using Building_Blocks.Behaviors;
using Building_Blocks.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Catalog.Tests;

public class LoggingBehaviorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Logging_records_request_type_and_duration_even_when_the_handler_throws(bool throws)
    {
        var logger = new RecordingLogger<LoggingBehavior<PrivateRequest, string>>();
        var behavior = new LoggingBehavior<PrivateRequest, string>(logger);
        using var cancellation = new CancellationTokenSource();
        var error = new InvalidOperationException("Handler failure");

        Task<string> Handle() => behavior.Handle(new PrivateRequest("Private request data"), token =>
        {
            Assert.Equal(cancellation.Token, token);
            return throws ? Task.FromException<string>(error) : Task.FromResult("response");
        }, cancellation.Token);

        if (throws)
            Assert.Same(error, await Assert.ThrowsAsync<InvalidOperationException>(Handle));
        else
            Assert.Equal("response", await Handle());

        Assert.Equal(2, logger.Messages.Count);
        Assert.Contains(nameof(PrivateRequest), logger.Messages[0]);
        Assert.Contains("ms", logger.Messages[1]);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("Private request data"));
    }

    [Fact]
    public async Task Request_cancellation_is_not_converted_to_a_server_error()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var context = new DefaultHttpContext { RequestAborted = cancellation.Token };
        var logger = new RecordingLogger<CustomExceptionHandler>();

        var handled = await new CustomExceptionHandler(logger)
            .TryHandleAsync(context, new OperationCanceledException(cancellation.Token), cancellation.Token);

        Assert.False(handled);
        Assert.Empty(logger.Messages);
    }

    private record PrivateRequest(string Secret);

    private class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
