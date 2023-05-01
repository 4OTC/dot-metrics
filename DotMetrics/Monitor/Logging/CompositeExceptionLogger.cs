namespace DotMetrics.Monitor.Logging
{
    public class CompositeExceptionLogger : IExceptionLogger
    {
        private readonly IExceptionLogger[] _delegates;

        public CompositeExceptionLogger(IExceptionLogger[] delegates)
        {
            _delegates = delegates;
        }

        public void OnExceptionStart(string exceptionType, string exceptionMessage)
        {
            foreach (IExceptionLogger @delegate in _delegates)
            {
                @delegate.OnExceptionStart(exceptionType, exceptionMessage);
            }
        }

        public void OnExceptionCaught(string methodName)
        {
            foreach (IExceptionLogger @delegate in _delegates)
            {
                @delegate.OnExceptionCaught(methodName);
            }
        }
    }
}