namespace DotMetrics.Monitor.Logging
{
    public interface IExceptionLogger
    {
        void OnExceptionCaught(string methodName);

        void OnExceptionStart(string exceptionType, string exceptionMessage);
    }
}