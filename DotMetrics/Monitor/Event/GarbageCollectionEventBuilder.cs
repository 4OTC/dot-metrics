using System;
using DotMetrics.Monitor.Publish;

namespace DotMetrics.Monitor.Event
{
    public class GarbageCollectionEventBuilder
    {
        private readonly IMetricsPublisher _metricsPublisher;
        private readonly string _serviceName;
        private long _suspendDurationMicros;
        private long _collectionDurationMicros;
        private long _unsuspendDurationMicros;
        private long _totalPauseTimeMicros;
        private DateTime _startEventTimestamp;
        private DateTime _suspendStartTimestamp = DateTime.MinValue;

        public GarbageCollectionEventBuilder(string serviceName, IMetricsPublisher metricsPublisher)
        {
            _metricsPublisher = metricsPublisher;
            _serviceName = serviceName;
        }

        public void OnSuspendStart(DateTime eventTime)
        {
            Reset();
            _suspendStartTimestamp = eventTime;
            _startEventTimestamp = eventTime;
        }

        public void OnSuspendStop(DateTime eventTime)
        {
            _suspendDurationMicros = TimeUtil.DateTimeDeltaToMicros(_startEventTimestamp, eventTime);
        }

        public void OnGcStart(DateTime eventTime)
        {
            _startEventTimestamp = eventTime;
        }

        public void OnGcStop(DateTime eventTime)
        {
            _collectionDurationMicros = TimeUtil.DateTimeDeltaToMicros(_startEventTimestamp, eventTime);
        }

        public void OnUnsuspendStart(DateTime eventTime)
        {
            _startEventTimestamp = eventTime;
        }

        public void OnUnsuspendStop(DateTime eventTime)
        {
            if (_suspendStartTimestamp != DateTime.MinValue)
            {
                _unsuspendDurationMicros = TimeUtil.DateTimeDeltaToMicros(_startEventTimestamp, eventTime);
                _totalPauseTimeMicros = TimeUtil.DateTimeDeltaToMicros(_suspendStartTimestamp, eventTime);
                _metricsPublisher.OnGarbageCollection(_serviceName, _suspendStartTimestamp, _suspendDurationMicros,
                    _collectionDurationMicros, _unsuspendDurationMicros, _totalPauseTimeMicros);
            }

            Reset();
        }

        private void Reset()
        {
            _suspendDurationMicros = 0;
            _collectionDurationMicros = 0;
            _unsuspendDurationMicros = 0;
            _totalPauseTimeMicros = 0;
            _suspendStartTimestamp = DateTime.MinValue;
        }
    }
}