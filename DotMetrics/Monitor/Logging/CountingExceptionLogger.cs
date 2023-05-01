using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Logging
{
    public class CountingExceptionLogger : IExceptionLogger
    {
        private readonly Dictionary<string, int> _countByMethodAndType = new();
        private readonly object _stateLock = new();
        private readonly ILogger _logger;
        private string _lastMethodName;

        public CountingExceptionLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void OnExceptionCaught(string methodName)
        {
            if (_lastMethodName != null)
            {
                _logger.LogWarning("Non-null value for lastMethodName");
            }
            _lastMethodName = methodName;
        }

        public void OnExceptionStart(string exceptionType, string exceptionMessage)
        {
            // first
            if (_lastMethodName == null)
            {
                _logger.LogWarning("No method name recorded when exception starting");
                return;
            }

            _ = _lastMethodName + "/" + exceptionType;
            lock (_stateLock)
            {
                // _countByMethodAndType.TryGetValue()
            }
        }

        public void Report(CountedExceptionListener exceptionListener)
        {

        }
    }
}