using System;
using System.Text;
using DotMetrics.Codec.Metrics;
using DotMetrics.Monitor.Publish;

namespace DotMetrics.Monitor.Daemon;

public class MetricPublisherMetricValueReceiver : IMetricValueReceiver
{
    private readonly IMetricsPublisher _metricsPublisher;
    private readonly string _serviceName;
    private readonly string _providerName;

    public MetricPublisherMetricValueReceiver(
        IMetricsPublisher metricsPublisher,
        string serviceName,
        string providerName)
    {
        _metricsPublisher = metricsPublisher;
        _serviceName = serviceName;
        _providerName = providerName;
    }

    public void Receive(ReadOnlySpan<byte> key, double value, long updateTimeEpochMs)
    {
        _metricsPublisher.OnRuntimeMetric(_serviceName,
            DateTime.UnixEpoch + TimeSpan.FromMilliseconds(updateTimeEpochMs), _providerName,
            Encoding.UTF8.GetString(key), value);
    }
}