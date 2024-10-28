using Abs.CommonCore.Platform.Extensions.Hosting;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Abs.CommonCore.Platform.Tests.Extensions.Hosting;

public class RetryingExecutionServiceTests
{
    private readonly Mock<ILogger<RetryingExecutionService>> _loggerMock;
    private readonly TestRetryingExecutionService _service;

    private Func<int, TimeSpan> _getRetryDelayMock;
    private Func<CancellationToken, Task<IAsyncDisposable>> _startExecutionMock;
    private Func<CancellationToken, Task>? _postExecutionMock;
    private int _getRetryDelayCallCount = 0;
    private int _startExecutionCallCount = 0;

    public RetryingExecutionServiceTests()
    {
        _loggerMock = new Mock<ILogger<RetryingExecutionService>>();
        _getRetryDelayMock = _ => TimeSpan.FromMilliseconds(3);
        _startExecutionMock = _ => Task.FromResult(Mock.Of<IAsyncDisposable>());

        _service = new TestRetryingExecutionService(_loggerMock.Object,
            () =>
            {
                _getRetryDelayCallCount++;
                return _getRetryDelayMock;
            },
            () =>
            {
                _startExecutionCallCount++;
                return _startExecutionMock;
            },
            () => _postExecutionMock);
    }

    [Fact(Timeout = 1_000)]
    public async Task ExecuteAsync_ShouldLogStartMessage()
    {
        // Arrange
        var stoppingTokenSource = new CancellationTokenSource();
        _postExecutionMock = ct => Task.Delay(1, ct);

        // Act
        await _service.ExecuteAsync(stoppingTokenSource.Token);

        // Assert
        _startExecutionCallCount.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact(Timeout = 3_000)]
    public async Task ExecuteAsync_ShouldRetryOnException()
    {
        // Arrange
        var stoppingTokenSource = new CancellationTokenSource();
        _startExecutionMock = _ => throw new Exception("Test exception");

        // Act
        var executionTask = _service.ExecuteAsync(stoppingTokenSource.Token);

        // Assert
        while (_getRetryDelayCallCount < 10)
        {
            await Task.Delay(10, CancellationToken.None);
        }

        await stoppingTokenSource.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await executionTask);
    }

    [Fact(Timeout = 1000)]
    public async Task ExecuteAsync_ShouldCancelOnOperationCanceledException()
    {
        // Arrange
        var stoppingTokenSource = new CancellationTokenSource();
        await stoppingTokenSource.CancelAsync();

        // Act
        var executionTask = _service.ExecuteAsync(stoppingTokenSource.Token);

        // Assert
        _getRetryDelayCallCount.Should().Be(0);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await executionTask);
    }

    public class TestRetryingExecutionService(
        ILogger logger,
        Func<Func<int, TimeSpan>> getRetryDelayMockProvider,
        Func<Func<CancellationToken, Task<IAsyncDisposable>>> startExecutionMockProvider,
        Func<Func<CancellationToken, Task>?> postExecutionMockProvider)
            : RetryingExecutionService(logger)
    {
        public new Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return base.ExecuteAsync(stoppingToken);
        }

        protected override Task PostExecution(CancellationToken cancellationToken)
        {
            var postExecutionMock = postExecutionMockProvider();
            return postExecutionMock?.Invoke(cancellationToken)
                   ?? base.PostExecution(cancellationToken);
        }

        protected override Task<IAsyncDisposable> StartExecutionAsync(CancellationToken cancellationToken)
        {
            // Provide a mock implementation for testing
            var startExecutionMock = startExecutionMockProvider();
            return startExecutionMock(cancellationToken);
        }

        protected override TimeSpan GetRetryDelay(int retryCount)
        {
            // Provide a mock implementation for testing
            var getRetryDelayMock = getRetryDelayMockProvider();
            return getRetryDelayMock(retryCount);
        }
    }
}
