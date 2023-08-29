using System;
using System.Threading;
using System.Threading.Tasks;
using DotMetrics.Codec.Metrics;
using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Daemon;

public class MetricRepositoryPoller
{
    private readonly IMetricRepository _metricRepository;
    private readonly IMetricValueReceiver _metricValueReceiver;
    private readonly TimeSpan _pollInterval;
    private readonly CancellationToken _cancellationToken;
    private readonly ILogger _applicationLogger;

    public MetricRepositoryPoller(
        IMetricRepository metricRepository,
        IMetricValueReceiver metricValueReceiver,
        TimeSpan pollInterval,
        ILogger applicationLogger,
        CancellationToken cancellationToken)
    {
        _metricRepository = metricRepository;
        _metricValueReceiver = metricValueReceiver;
        _pollInterval = pollInterval;
        _cancellationToken = cancellationToken;
        _applicationLogger = applicationLogger;
    }

    public void Run()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            Task.Delay(_pollInterval, _cancellationToken).GetAwaiter().GetResult();
            try
            {
                _metricRepository.Read(_metricValueReceiver);
            }
            catch (Exception e)
            {
                _applicationLogger.LogError($"Failed to read from repository: {e}");
            }
        }
    }
}