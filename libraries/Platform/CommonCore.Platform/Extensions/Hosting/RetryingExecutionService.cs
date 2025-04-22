using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Policy = Polly.Policy;

namespace Abs.CommonCore.Platform.Extensions.Hosting;

/// <summary>
/// An abstract background service that implements a retry mechanism for executing tasks.
/// </summary>
public abstract class RetryingExecutionService(ILogger logger) : BackgroundService
{
    /// <summary>
    /// Executes the background service with a retry policy.
    /// </summary>
    /// <param name="stoppingToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the execution of the background service.</returns>
    protected sealed override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker execution started");

        var retryPolicy = Policy.Handle<Exception>()
            .WaitAndRetryForeverAsync(
                sleepDurationProvider: GetRetryDelay,
                onRetry: (exception, retryCount, calculatedWaitDuration) =>
                {
                    if (exception is OperationCanceledException)
                    {
                        logger.LogInformation("Worker execution loop cancelled");
                        return;
                    }

                    logger.LogError(
                        $"Retrying in {calculatedWaitDuration.TotalSeconds} seconds (Reason: {exception.Message}) (Retry count: {retryCount})");
                });

        return retryPolicy.ExecuteAsync(async cancellationToken =>
        {
            await using var execution = await StartExecutionAsync(cancellationToken);

            await PostExecution(cancellationToken);
        }, stoppingToken);
    }

    /// <summary>
    /// Performs any post-execution tasks.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the post-execution process.</returns>
    protected virtual Task PostExecution(CancellationToken cancellationToken)
    {
        return Task.Delay(Timeout.Infinite, cancellationToken);
    }

    /// <summary>
    /// Gets the delay duration before the next retry attempt.
    /// </summary>
    /// <param name="retryCount">The current retry count.</param>
    /// <returns>A TimeSpan representing the delay duration.</returns>
    protected abstract TimeSpan GetRetryDelay(int retryCount);

    /// <summary>
    /// Starts the execution process.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the start of the execution process.</returns>
    protected abstract Task<IAsyncDisposable> StartExecutionAsync(CancellationToken cancellationToken);
}
