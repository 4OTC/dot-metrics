using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotMetrics.Monitor.Alerting;
using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Monitoring;

public class MonitoringDaemon
{
    private static readonly string EnvironmentName =
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    private readonly CancellationToken _cancellationToken;
    private readonly IAlertSender _alertSender;
    private readonly List<IAlertRule> _alertRules;
    private readonly ILogger _logger;

    public MonitoringDaemon(IAlertSender alertSender,
        List<IAlertRule> alertRules,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _alertSender = alertSender;
        _alertRules = alertRules;
        _logger = logger;
    }

    public async Task Run()
    {
        _logger.LogInformation("Starting monitoring daemon");
        while (!_cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);

            foreach (IAlertRule alertRule in _alertRules)
            {
                if (alertRule.IsTriggered())
                {
                    string alertMessage = $"{Environment.MachineName}/{EnvironmentName}: {alertRule.GetAlertMessage()}";
                    _logger.LogInformation($"Sending alert: {alertMessage}");
                    await _alertSender.Send(alertMessage);
                }
            }
        }
        _logger.LogInformation("Stopping monitoring daemon");
    }
}