using System;

namespace DotMetrics.Codec.Metrics;

public interface IMetricValueReceiver
{
    void Receive(ReadOnlySpan<byte> key, double value, long updateTimeEpochMs);
}