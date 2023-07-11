namespace DotMetrics.Codec.Test.Metrics;

public struct MetricValue
{
    public readonly long UpdateTimeEpochMs;
    public readonly double Value;

    public MetricValue(double value, long updateTimeEpochMs) : this()
    {
        Value = value;
        UpdateTimeEpochMs = updateTimeEpochMs;
    }
}