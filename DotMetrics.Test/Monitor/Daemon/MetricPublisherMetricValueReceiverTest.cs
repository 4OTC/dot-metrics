using System;
using System.Text;
using DotMetrics.Monitor.Daemon;
using DotMetrics.Monitor.Publish;
using Moq;
using Xunit;

namespace DotMetrics.Test.Monitor.Daemon;

public class MetricPublisherMetricValueReceiverTest
{
    private const string MetricName = "metric-one";
    private const string ServiceName = "service";
    private const string ProviderName = "provider";

    private readonly Mock<IMetricsPublisher> _metricPublisherMock = new();
    private readonly MetricPublisherMetricValueReceiver _receiver;

    public MetricPublisherMetricValueReceiverTest()
    {
        _receiver = new MetricPublisherMetricValueReceiver(_metricPublisherMock.Object, ServiceName, ProviderName);
    }

    [Fact]
    public void ShouldPublishMetricValue()
    {
        DateTime currentTime = new DateTime(2023, 1, 1, 13, 14, 15, 678);
        long timeOne = (long)(currentTime - DateTime.UnixEpoch).TotalMilliseconds;
        _receiver.Receive(Encoding.UTF8.GetBytes(MetricName), 17, timeOne);

        _metricPublisherMock.Verify(
            x => x.OnRuntimeMetric(ServiceName, currentTime, ProviderName, MetricName, 17),
            Times.Once);
    }
}