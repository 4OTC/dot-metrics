using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using DotMetrics.Monitor.Configuration;
using DotMetrics.Monitor.Publish;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace DotMetrics.Monitor.Event
{
    public class SystemRuntimeEventHandler : IDynamicEventHandler
    {
        private const string SystemRuntimeProviderName = "System.Runtime";

        private static readonly HashSet<string> EventsToReport = new(new[]
        {
            "working-set",
            "cpu-usage",
            "gc-heap-size",
            "gen-0-gc-count",
            "gen-1-gc-count",
            "gen-2-gc-count",
            "time-in-gc",
            "loh-size",
            "gen-0-size",
            "gen-1-size",
            "gen-2-size",
            "alloc-rate",
            "gc-fragmentation",
            "exception-count",
            "threadpool-thread-count",
            "monitor-lock-contention-count",
            "threadpool-queue-length",
            "threadpool-completed-items-count",
            "active-timer-count",
        });

        private readonly string _serviceName;
        private readonly IMetricsPublisher _metricsPublisher;
        private readonly EnvironmentConfiguration _environmentConfiguration;

        public SystemRuntimeEventHandler(
            string serviceName, 
            IMetricsPublisher metricsPublisher, 
            EnvironmentConfiguration environmentConfiguration = null)
        {
            _serviceName = serviceName;
            _metricsPublisher = metricsPublisher;
            _environmentConfiguration = environmentConfiguration ?? EnvironmentConfiguration.GetInstance();
        }

        public EventPipeProvider GetProvider()
        {
            var dictionary = new Dictionary<string, string>
            {
                ["EventCounterIntervalSec"] = _environmentConfiguration.PollIntervalSeconds.ToString()
            };
            return new EventPipeProvider(SystemRuntimeProviderName, EventLevel.Verbose, 0xFFFFFFFF, dictionary);
        }

        public void HandleEvent(TraceEvent traceEvent)
        {
            if (traceEvent.EventName == "EventCounters")
            {
                IDictionary<string, object> eventData = (IDictionary<string, object>)(traceEvent.PayloadValue(0));
                IDictionary<string, object> countersPayload =
                    (IDictionary<string, object>)(eventData["Payload"]);

                string name = countersPayload["Name"].ToString();
                string counterType = countersPayload["CounterType"].ToString();

                if (EventsToReport.Contains(name))
                {
                    string valueKey = null;
                    if (counterType == "Sum")
                    {
                        valueKey = "Increment";
                    }
                    else if (counterType == "Mean")
                    {
                        valueKey = "Mean";
                    }

                    if (valueKey != null)
                    {
                        double value = double.Parse(countersPayload[valueKey].ToString());
                        _metricsPublisher.OnRuntimeMetric(_serviceName, DateTime.UtcNow, traceEvent.ProviderName, name,
                            value);
                    }
                }
            }
        }
    }
}