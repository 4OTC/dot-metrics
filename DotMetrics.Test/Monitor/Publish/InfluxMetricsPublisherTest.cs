using System;
using System.Collections.Generic;
using DotMetrics.Monitor.Publish;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotMetrics.Test.Monitor.Publish
{
    public class InfluxMetricsPublisherTest
    {
        private const int ContentionDurationMicros = 117;
        private const int ExceptionDurationMicros = 911;
        private const string ServiceName = "service-name";
        private const int SuspendDurationMicros = 873;
        private const int CollectionMicros = 78222;
        private const int UnsuspendMicros = 49;
        private const int TotalMicros = 81223;
        private const string ProviderName = "Provider.Name";
        private const string MetricOne = "metric-1";
        private const string MetricTwo = "metric-2";
        private const string DotNetRuntimeNamespace = "DotNetRuntime";

        private readonly Mock<InfluxMetricsPublisher.MetricWriter> _collectorMock = new();
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly InfluxMetricsPublisher _publisher;

        public InfluxMetricsPublisherTest()
        {
            _publisher = new InfluxMetricsPublisher(_collectorMock.Object, _loggerMock.Object);
        }

        private struct Invocation
        {
            internal string Measurement;
            internal Dictionary<string, string> Tags;
            internal Dictionary<string, object> Data;
            internal DateTime Timestamp;
        }

        [Fact]
        public void ShouldPublishEvents()
        {
            var invocations = new List<Invocation>();
            _collectorMock.Setup(x => x(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(),
                It.IsAny<Dictionary<string, string>>(), It.IsNotNull<DateTime>())).Callback(
                (string measurement, Dictionary<string, object> data, Dictionary<string, string> tags, DateTime? timestamp) =>
                    invocations.Add(new Invocation()
                    {
                        Measurement = measurement,
                        Tags = new Dictionary<string, string>(tags),
                        Data = new Dictionary<string, object>(data),
                        Timestamp = timestamp ?? DateTime.Now
                    }));

            _publisher.OnContention(ServiceName, DateTime.Now, ContentionDurationMicros);
            _publisher.OnException(ServiceName, DateTime.Now, ExceptionDurationMicros);

            _publisher.OnRuntimeMetric(ServiceName, DateTime.Now, ProviderName, MetricOne, double.MaxValue);
            _publisher.OnGarbageCollection(ServiceName, DateTime.Now, SuspendDurationMicros, CollectionMicros, UnsuspendMicros, TotalMicros);
            _publisher.OnRuntimeMetric(ServiceName, DateTime.Now, ProviderName, MetricTwo, double.MinValue);

            ExpectMeasurement(DotNetRuntimeNamespace + ".Contention", ContentionDurationMicros, invocations[0]);
            ExpectMeasurement(DotNetRuntimeNamespace + ".Exception", ExceptionDurationMicros, invocations[1]);
            ExpectMeasurement(ProviderName + "." + MetricOne, double.MaxValue, invocations[2]);
            ExpectMeasurement(DotNetRuntimeNamespace + ".Gc.Suspend", SuspendDurationMicros, invocations[3]);
            ExpectMeasurement(DotNetRuntimeNamespace + ".Gc.Collect", CollectionMicros, invocations[4]);
            ExpectMeasurement(DotNetRuntimeNamespace + ".Gc.Unsuspend", UnsuspendMicros, invocations[5]);
            ExpectMeasurement(DotNetRuntimeNamespace + ".Gc.Total", TotalMicros, invocations[6]);
            ExpectMeasurement(ProviderName + "." + MetricTwo, double.MinValue, invocations[7]);

        }

        private static void ExpectMeasurement(string metricName, double value, Invocation invocation)
        {
            Assert.Equal(metricName, invocation.Measurement);
            Assert.Equal(value, (double)invocation.Data["value"]);
            Assert.Equal(Environment.MachineName, invocation.Tags["host"]);
            Assert.Equal(ServiceName, invocation.Tags["service"]);
        }
    }
}