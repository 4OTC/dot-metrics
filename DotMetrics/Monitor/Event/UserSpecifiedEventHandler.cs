using System;
using System.Collections.Generic;
using DotMetrics.Monitor.Publish;
using Microsoft.Diagnostics.Tracing;

namespace DotMetrics.Monitor.Event
{
    public class UserSpecifiedEventHandler
    {
        private readonly string _serviceName;
        private readonly IMetricsPublisher _metricsPublisher;

        public UserSpecifiedEventHandler(string serviceName, IMetricsPublisher metricsPublisher)
        {
            _serviceName = serviceName;
            _metricsPublisher = metricsPublisher;
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