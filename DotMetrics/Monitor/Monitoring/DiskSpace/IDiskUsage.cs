namespace DotMetrics.Monitor.Monitoring.DiskSpace;

public interface IDiskUsage
{
    decimal GetUtilisationPercentage();
}