namespace DotMetrics.Monitor.Logging
{
    public class NoOpExceptionLogger : IExceptionLogger
    {
        public static readonly IExceptionLogger Instance = new NoOpExceptionLogger();

        public void OnExceptionCaught(string methodName)
        {
        }

        public void OnExceptionStart(string exceptionType, string exceptionMessage)
        {
        }
    }
}