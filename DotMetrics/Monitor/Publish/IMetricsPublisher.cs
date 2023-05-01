using System;

namespace DotMetrics.Monitor.Publish
{
    public interface IMetricsPublisher
    {
        void OnContention(
            string service,
            DateTime eventTime,
            long durationMicros);

        void OnGarbageCollection(
            string service,
            DateTime eventTime,
            long suspendDurationMicros,
            long collectionMicros,
            long unsuspendMicros,
            long totalMicros);

        void OnException(
            string service,
            DateTime eventTime,
            long processingDurationMicros);

        void OnRuntimeMetric(
            string service,
            DateTime eventTime,
            string provider,
            string metric,
            double value);
    }
}