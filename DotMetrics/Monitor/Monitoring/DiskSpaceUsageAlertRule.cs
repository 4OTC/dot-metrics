using System;
using DotMetrics.Monitor.Monitoring.DiskSpace;

namespace DotMetrics.Monitor.Monitoring;

public class DiskSpaceUsageAlertRule : IAlertRule
{
    private readonly string _mountPoint;
    private readonly decimal _utilisationThresholdPercentage;
    private readonly IDiskUsage _diskUsage;
    private bool _alertSent = false;

    public DiskSpaceUsageAlertRule(string mountPoint, decimal utilisationThresholdPercentage,
        Func<string, IDiskUsage> factory)
    {
        _mountPoint = mountPoint;
        _utilisationThresholdPercentage = utilisationThresholdPercentage;
        _diskUsage = factory(mountPoint);
    }

    public bool IsTriggered()
    {
        bool usageOverThreshold = _diskUsage.GetUtilisationPercentage() >= _utilisationThresholdPercentage;
        if (!usageOverThreshold)
        {
            _alertSent = false;
        }

        if (usageOverThreshold && !_alertSent)
        {
            _alertSent = true;
            return true;
        }

        return false;
    }

    public string GetAlertMessage()
    {
        return
            $"Disk usage for volume {_mountPoint} is at {_diskUsage.GetUtilisationPercentage().ToString("F2")}%, alert threshold is {_utilisationThresholdPercentage.ToString("F2")}%";
    }
}