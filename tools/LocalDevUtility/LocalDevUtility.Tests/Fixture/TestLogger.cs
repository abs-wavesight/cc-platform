using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Abs.CommonCore.LocalDevUtility.Tests.Fixture;

public class TestLogger : ILogger
{
    private ITestOutputHelper? _testOutputHelper;

    private static TestLogger? _default;
    public static TestLogger Default
    {
        get
        {
            if (_default == null)
            {
                _default = new TestLogger();
            }

            return _default;
        }
    }

    public TestLogger(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public TestLogger()
    {
    }

    public void SetTestOutput(ITestOutputHelper output)
    {
        _testOutputHelper = output;
    }

    public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Log(logLevel, eventId, state, exception, formatter, "Unknown");
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter, string category)
    {
        var formattedMessage = $"{DateTime.Now:u} [{category}] [{logLevel.ToString()}] {formatter(state, exception)}";
        try
        {
            _testOutputHelper?.WriteLine(formattedMessage);
        }
        catch (Exception)
        {
            System.Console.WriteLine(formattedMessage);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}

public class TestLogger<T> : TestLogger, ILogger<T>
{
    public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Default.Log(logLevel, eventId, state, exception, formatter, typeof(T).FullName!);
    }
}
