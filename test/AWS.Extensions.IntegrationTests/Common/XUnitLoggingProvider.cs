using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.Common;

[ExcludeFromCodeCoverage]
public class XUnitLoggingProvider : ILoggerProvider, ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitLoggingProvider(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        _testOutputHelper.WriteLine($"[{logLevel}]: {formatter(state, exception)}");
    }

    public ILogger CreateLogger(string categoryName) => this;

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public void Dispose() { }
}
