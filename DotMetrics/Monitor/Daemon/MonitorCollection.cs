using System.Collections.Generic;
using System.Threading;
using DotMetrics.Monitor.Configuration;
using DotMetrics.Monitor.Logging;
using DotMetrics.Monitor.Publish;
using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Daemon
{
    public class MonitorCollection
    {
        private readonly ProcessInfo[] _monitoredProcesses;
        private readonly List<string> _providerNames;
        private readonly IMetricsPublisher _metricsPublisher;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly ILogger _applicationLogger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Thread[] _threads;
        private readonly EnvironmentConfiguration _environmentConfiguration;

        public MonitorCollection(
            ProcessInfo[] monitoredProcesses,
            List<string> providerNames,
            IMetricsPublisher metricsPublisher,
            IExceptionLogger exceptionLogger,
            ILogger applicationLogger,
            CancellationTokenSource cancellationTokenSource,
            EnvironmentConfiguration environmentConfiguration = null)
        {
            _monitoredProcesses = monitoredProcesses;
            _providerNames = providerNames;
            _metricsPublisher = metricsPublisher;
            _exceptionLogger = exceptionLogger;
            _applicationLogger = applicationLogger;
            _cancellationTokenSource = cancellationTokenSource;
            _threads = new Thread[monitoredProcesses.Length];
            _environmentConfiguration = environmentConfiguration ?? EnvironmentConfiguration.GetInstance();
        }

        public void Run()
        {
            for (int index = 0; index < _monitoredProcesses.Length; index++)
            {
                ProcessInfo processInfo = _monitoredProcesses[index];
                MetricsMonitor monitor = new MetricsMonitor(
                    processInfo,
                    _providerNames,
                    _metricsPublisher,
                    _exceptionLogger,
                    _applicationLogger,
                    _cancellationTokenSource,
                    _environmentConfiguration);
                Thread thread = new Thread(monitor.Run)
                {
                    IsBackground = true,
                    Name = $"dot-metrics-monitor-{index}"
                };
                _threads[index] = thread;
                thread.Start();
            }
        }
    }
}