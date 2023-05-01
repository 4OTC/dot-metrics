namespace DotMetrics.Monitor.Monitoring;

public interface IAlertRule
{
    bool IsTriggered();

    string GetAlertMessage();
}