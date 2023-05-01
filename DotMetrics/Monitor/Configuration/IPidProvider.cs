namespace DotMetrics.Monitor.Configuration
{
    public interface IPidProvider
    {
        ProcessInfo[] GetMonitoredProcesses();
    }
}