using System;
using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Logging
{
    public class NoOpLogger : ILogger
    {
        public static readonly ILogger Instance = new NoOpLogger();

#nullable enable
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
#nullable disable

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NullDisposable();
        }

        private class NullDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}