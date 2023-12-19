using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace Trdr;

internal sealed class NullLogger : ILogger

{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
       // NOP
    }

    public bool IsEnabled(LogLevel logLevel) => false;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return Disposable.Empty;
    }
}