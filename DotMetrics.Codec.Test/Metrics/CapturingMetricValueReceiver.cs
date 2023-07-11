using System.Text;
using DotMetrics.Codec.Metrics;

namespace DotMetrics.Codec.Test.Metrics;

public class CapturingMetricValueReceiver : IMetricValueReceiver
{
    public readonly Dictionary<string, MetricValue> CapturedMetrics = new();

    public void Receive(ReadOnlySpan<byte> key, double value, long updateTimeEpochMs)
    {
        CapturedMetrics[Encoding.UTF8.GetString(key)] = new MetricValue(value, updateTimeEpochMs);
    }
}