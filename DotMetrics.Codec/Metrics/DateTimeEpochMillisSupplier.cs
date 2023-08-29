using System;

namespace DotMetrics.Codec.Metrics;

public class DateTimeEpochMillisSupplier : IEpochMillisSupplier
{
    public static readonly DateTimeEpochMillisSupplier Instance = new DateTimeEpochMillisSupplier();

    private DateTimeEpochMillisSupplier()
    {
    }

    public long EpochMs()
    {
        return (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds;
    }
}