using System;
using System.Collections.Generic;
using DotMetrics.Monitor.Configuration;
using InfluxDB.Collector;
using InfluxDB.Collector.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Publish
{
    public class InfluxMetricsPublisher : IMetricsPublisher
    {
        private const string ServiceKey = "service";
        private const string ValueKey = "value";
        private const string HostKey = "host";
        private const string DotNetRuntimePrefix = "DotNetRuntime";
        private const string ContentionMeasurement = DotNetRuntimePrefix + ".Contention";
        private const string ExceptionMeasurement = DotNetRuntimePrefix + ".Exception";
        private const string GcSuspendMeasurement = DotNetRuntimePrefix + ".Gc.Suspend";
        private const string GcCollectMeasurement = DotNetRuntimePrefix + ".Gc.Collect";
        private const string GcUnsuspendMeasurement = DotNetRuntimePrefix + ".Gc.Unsuspend";
        private const string GcTotalMeasurement = DotNetRuntimePrefix + ".Gc.Total";
        private readonly Dictionary<string, string> _tags = new();
        private readonly Dictionary<string, object> _data = new();
        private readonly MetricWriter _metricWriter;

        public InfluxMetricsPublisher(
            InfluxDbConfiguration configuration,
            ILogger logger) : this(new CollectorConfiguration()
            .Batch.AtInterval(TimeSpan.FromSeconds(10))
            .WriteTo.InfluxDB(
                configuration.Url,
                configuration.Database,
                configuration.Username,
                configuration.Password)
            .CreateCollector().Write, logger)
        {
        }

        public InfluxMetricsPublisher(MetricWriter metricWriter, ILogger logger)
        {
            _tags[HostKey] = Environment.MachineName;
            _metricWriter = metricWriter;
            CollectorLog.RegisterErrorHandler((msg, exception) =>
            {
                logger.LogWarning(exception, $"Caught exception from influx client: {msg}");
            });
        }

        public void OnContention(string service, DateTime eventTime, long durationMicros)
        {
            _tags[ServiceKey] = service;
            SetDataValue(durationMicros);
            _metricWriter(ContentionMeasurement, _data, _tags, eventTime);
        }

        public void OnGarbageCollection(
            string service, DateTime eventTime,
            long suspendDurationMicros,
            long collectionMicros,
            long unsuspendMicros,
            long totalMicros)
        {
            _tags[ServiceKey] = service;
            SetDataValue(suspendDurationMicros);
            _metricWriter(GcSuspendMeasurement, _data, _tags, eventTime);
            SetDataValue(collectionMicros);
            _metricWriter(GcCollectMeasurement, _data, _tags, eventTime);
            SetDataValue(unsuspendMicros);
            _metricWriter(GcUnsuspendMeasurement, _data, _tags, eventTime);
            SetDataValue(totalMicros);
            _metricWriter(GcTotalMeasurement, _data, _tags, eventTime);
        }

        public void OnException(string service, DateTime eventTime, long processingDurationMicros)
        {
            _tags[ServiceKey] = service;
            SetDataValue(processingDurationMicros);
            _metricWriter(ExceptionMeasurement, _data, _tags, eventTime);
        }

        public void OnRuntimeMetric(string service, DateTime eventTime, string provider, string metric, double value)
        {
            _tags[ServiceKey] = service;
            _data[ValueKey] = value;
            _metricWriter($"{provider}.{metric}", _data, _tags, eventTime);
        }

        public delegate void MetricWriter(string name, Dictionary<string, object> data, Dictionary<string, string> tags,
            DateTime? timestamp);

        private void SetDataValue(long durationMicros)
        {
            _data[ValueKey] = (double)durationMicros;
        }
    }
}