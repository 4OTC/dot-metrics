using System;
using DotMetrics.Monitor.Publish;

namespace DotMetrics.Monitor.Event
{
    public class ExceptionEventBuilder
    {
        private readonly string _serviceName;
        private readonly IMetricsPublisher _metricsPublisher;
        private DateTime _startTimestamp = DateTime.MinValue;

        public ExceptionEventBuilder(string serviceName, IMetricsPublisher metricsPublisher)
        {
            _serviceName = serviceName;
            _metricsPublisher = metricsPublisher;
        }

        public void OnExceptionStart(DateTime eventTime)
        {
            _startTimestamp = eventTime;
        }

        public void OnExceptionStop(DateTime eventTime)
        {
            if (_startTimestamp != DateTime.MinValue)
            {
                _metricsPublisher.OnException(_serviceName, _startTimestamp,
                    TimeUtil.DateTimeDeltaToMicros(_startTimestamp, eventTime));
            }
            _startTimestamp = DateTime.MinValue;
        }
    }
}