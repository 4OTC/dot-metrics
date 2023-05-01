using System;

namespace DotMetrics.Test.Monitor.Event
{
    internal class IncrementingClock
    {
        internal readonly DateTime StartTime = DateTime.Now;
        internal DateTime DateTime;

        internal IncrementingClock()
        {
            DateTime = StartTime;
        }

        internal DateTime SampleTime(int deltaMillis)
        {
            DateTime = DateTime.Add(TimeSpan.FromMilliseconds(deltaMillis));
            return DateTime;
        }
    }
}