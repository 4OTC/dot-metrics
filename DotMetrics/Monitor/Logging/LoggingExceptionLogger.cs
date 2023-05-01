using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Logging
{
    public class LoggingExceptionLogger : IExceptionLogger
    {
        private readonly ILogger _logger;

        public LoggingExceptionLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void OnExceptionCaught(string methodName)
        {
            _logger.LogInformation($"Exception caught from monitored process at: {methodName}");
        }

        public void OnExceptionStart(string exceptionType, string exceptionMessage)
        {
            _logger.LogInformation($"Exception start; type: {exceptionType}, message: {exceptionMessage}");
        }
    }
}