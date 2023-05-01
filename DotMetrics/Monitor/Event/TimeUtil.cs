using System;

namespace DotMetrics.Monitor.Event
{
    internal static class TimeUtil
    {
        private const long TicksPerMicrosecond = TimeSpan.TicksPerMillisecond / 1000;

        internal static long DateTimeDeltaToMicros(DateTime start, DateTime end)
        {
            return end.Subtract(start).Ticks / TicksPerMicrosecond;
        }
    }
}