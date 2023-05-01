namespace DotMetrics.Monitor.Configuration
{
    public class StaticPidProvider : IPidProvider
    {
        private readonly ProcessInfo[] _processInfoArray;

        public StaticPidProvider(ProcessInfo[] processInfoArray)
        {
            _processInfoArray = processInfoArray;
        }

        public ProcessInfo[] GetMonitoredProcesses()
        {
            return _processInfoArray;
        }
    }
}