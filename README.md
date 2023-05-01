# dot-metrics

A library containing utilities for collecting and publishing metrics using the `Dotnet CLR` `EventPipe` mechanism.

## Usage

`DotMetrics` is not opinionated about how it should be run, rather this is a concern of a wrapper application, which needs to be supplied by the user.

At a minimum, `DotMetrics` requires the PID of the `dotnet` process to be monitored. This is then supplied to a monitoring daemon, along with a set of required event sources.

When metrics are received from the monitored process, they are passed to the configured metrics publisher.

### Example minimal app

This minimal example uses `systemctl` on Linux to resolve a process PID, and publishes the metrics to an Influx database:

```
static class Program
{
    static async Task Main(string[] args)
    {
        using IHost host = CreateHostBuilder(args).Build();
        ILogger logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DotMetricsApp");
        string url = "http://influx-db.monitoring.lan";
        string username = "influx_write_user";
        string password = "______";
        string database = "dot-metrics";

        IMetricsPublisher metricsPublisher = new InfluxMetricsPublisher(new InfluxDbConfiguration(
            url, username, password, database), logger);
        LoggingExceptionLogger exceptionLogger = new LoggingExceptionLogger(logger);
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        CancellationToken hostCancellationToken = cancellationTokenSource.Token;

        string[] serviceName = new[] { "monitored-application-service" };
        logger.LogDebug($"Monitoring systemd services {string.Join(", ", serviceName)}");
        var monitoredProcesses = new SystemCtlPidProvider(serviceName, new ProcessExecutor())
            .GetMonitoredProcesses();
        Task monitoringTask = Task.Run(new MetricsMonitor(monitoredProcesses[0],
            new List<string.() { "application-metrics" },
            metricsPublisher, exceptionLogger, logger,
            cancellationTokenSource).Run, hostCancellationToken);
        logger.LogInformation("Components started");
        await Task.WhenAny(new[]
        {
            host.RunAsync(hostCancellationToken), monitoringTask
        });
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args).ConfigureLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
        });
}
``` 

The metrics publisher will receive user-generated events in the `application-metrics` namespace, in addition to the runtime-provided events from the CLR.

## User-generated Events

Applications can emit arbitrary named counter values using the `EventSource` utility provided by the `dotnet` runtime. 

This is achieved by extending the `EventSource` class:

```
[EventSource(Name="application-metrics")
public class AppEventSource : EventSource {
    private readonly PollingCounter _appMetricCounter;

    private double _metricOneValue;

    public AppEventSource() : base("application-metrics", EventSourceSettings.EtwSelfDescribingEventFormat) {
        _appMetricCounter = new PollingCounter("metric-1", this, () => _metricOneValue);
    }

    public void UpdateMetricOne(double newValue) {
        _metricOneValue = newValue;
    }
}
```

`DotMetrics` will pick up the latest value for the user-generated metrics and supply them to the metrics publisher.

