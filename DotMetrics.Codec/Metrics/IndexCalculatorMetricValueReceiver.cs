using System;

namespace DotMetrics.Codec.Metrics;

public class IndexCalculatorMetricValueReceiver : IMetricValueReceiver
{
    internal const int NotFound = -1;
    internal int MatchedIndex = NotFound;
    internal bool Found => MatchedIndex != NotFound;
    private readonly byte[] _requiredKey = new byte[128];
    private int _currentIndex = 0;
    private int _requiredKeyLength = 0;

    internal void Reset(ReadOnlySpan<byte> requiredKey)
    {
        _currentIndex = 0;
        MatchedIndex = NotFound;
        requiredKey.CopyTo(_requiredKey);
        _requiredKeyLength = requiredKey.Length;
    }

    public void Receive(ReadOnlySpan<byte> key, double value, long updateTimeEpochMs)
    {
        if (MatchedIndex == NotFound &&
            key.SequenceEqual(new ReadOnlySpan<byte>(_requiredKey, 0, _requiredKeyLength)))
        {
            MatchedIndex = _currentIndex;
        }
        _currentIndex++;
    }
}