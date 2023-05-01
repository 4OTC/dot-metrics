using System;

namespace DotMetrics.Monitor.Publish
{
    public class ConsoleMetricsPublisher : IMetricsPublisher
    {
        public void OnContention(string service, DateTime eventTime, long durationMicros)
        {
            Console.WriteLine($"{eventTime} {service} Contention; duration: {durationMicros}us");
        }

        public void OnGarbageCollection(string service, DateTime eventTime, long suspendDurationMicros, long collectionMicros,
            long unsuspendMicros, long totalMicros)
        {
            Console.WriteLine(
                $"{eventTime} {service} Garbage Collection; suspend: {suspendDurationMicros}us, " +
                $"collection: {collectionMicros}us, unsuspend: {unsuspendMicros}us, total: {totalMicros}us");
        }

        public void OnException(string service, DateTime eventTime, long processingDurationMicros)
        {
            Console.WriteLine($"{eventTime} {service} Exception; duration: {processingDurationMicros}us");
        }

        public void OnRuntimeMetric(string service, DateTime eventTime, string provider, string metric, double value)
        {
            Console.WriteLine($"{eventTime} {service} Metric; name: {provider}.{metric}, value: {value}");
        }
    }
}