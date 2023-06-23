using System;

namespace DotMetrics.Monitor.Configuration;

public class EnvironmentConfiguration
{
    private const int DefaultPollIntervalSeconds = 5;

    private EnvironmentConfiguration()
    {
    }

    public static EnvironmentConfiguration GetInstance() => new EnvironmentConfiguration();

    public int PollIntervalSeconds =>
        int.Parse(Environment.GetEnvironmentVariable("DOT_METRICS_POLL_INTERVAL_SECONDS") ??
                  DefaultPollIntervalSeconds.ToString());
}