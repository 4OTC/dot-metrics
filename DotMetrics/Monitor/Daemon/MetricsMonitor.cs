using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using DotMetrics.Monitor.Configuration;
using DotMetrics.Monitor.Event;
using DotMetrics.Monitor.Logging;
using DotMetrics.Monitor.Publish;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Extensions.Logging;

namespace DotMetrics.Monitor.Daemon
{
    public class MetricsMonitor
    {
        private const string DotnetRuntimeProviderName = "Microsoft-Windows-DotNETRuntime";

        private static readonly long[] EventIds = new[]
        {
            (long) ClrTraceEventParser.Keywords.Contention,
            (long) ClrTraceEventParser.Keywords.Exception,
            (long) ClrTraceEventParser.Keywords.GC,
        };

        private readonly int _monitoredProcessId;
        private readonly Dictionary<long, TraceEventHandler> _eventHandlerByCode = new();
        private readonly ProcessInfo _monitoredProcess;
        private readonly List<string> _providerNames;
        private readonly IMetricsPublisher _metricsPublisher;
        private readonly IExceptionLogger _exceptionLogger;
        private readonly GarbageCollectionEventBuilder _garbageCollectionEventBuilder;
        private readonly Dictionary<int, ExceptionEventBuilder> _exceptionEventBuilders = new();
        private readonly Dictionary<string, TraceEventHandler> _dynamicHandlers = new();
        private readonly ILogger _applicationLogger;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public MetricsMonitor(
            ProcessInfo monitoredProcess,
            List<string> providerNames,
            IMetricsPublisher metricsPublisher,
            IExceptionLogger exceptionLogger,
            ILogger applicationLogger,
            CancellationTokenSource cancellationTokenSource)
        {
            _monitoredProcessId = monitoredProcess.Pid;
            _monitoredProcess = monitoredProcess;
            _providerNames = providerNames;
            _metricsPublisher = metricsPublisher;
            _exceptionLogger = exceptionLogger;
            _applicationLogger = applicationLogger;
            _cancellationTokenSource = cancellationTokenSource;
            _eventHandlerByCode[(long)ClrTraceEventParser.Keywords.Contention] = ContentionEventHandler;
            _eventHandlerByCode[(long)ClrTraceEventParser.Keywords.Exception] = ExceptionEventHandler;
            _eventHandlerByCode[(long)ClrTraceEventParser.Keywords.GC] = GcEventHandler;
            _garbageCollectionEventBuilder = new(monitoredProcess.Label, _metricsPublisher);
        }

        public void Run()
        {
            long subscriptionFlags = 0;
            foreach (long eventId in EventIds)
            {
                subscriptionFlags |= eventId;
            }

            var providers = new List<EventPipeProvider>()
            {
                new(DotnetRuntimeProviderName,
                    EventLevel.Informational, subscriptionFlags),
            };

            SystemRuntimeEventHandler systemRuntimeHandler = new SystemRuntimeEventHandler(_monitoredProcess.Label, _metricsPublisher);
            EventPipeProvider provider = systemRuntimeHandler.GetProvider();
            _dynamicHandlers[provider.Name] = systemRuntimeHandler.HandleEvent;
            providers.Add(provider);
            if (_providerNames.Count != 0)
            {
                UserSpecifiedEventHandler userSpecifiedEventHandler = new UserSpecifiedEventHandler(_monitoredProcess.Label, _metricsPublisher);
                foreach (string providerName in _providerNames)
                {
                    providers.Add(new(providerName, EventLevel.Verbose, 0, new Dictionary<string, string>()
                    {
                        {"EventCounterIntervalSec", "5"}
                    }));
                    _dynamicHandlers[providerName] = userSpecifiedEventHandler.HandleEvent;
                    _applicationLogger.LogInformation($"Added handler for provider {providerName}");
                }
            }

            DiagnosticsClient diagnosticsClient = new DiagnosticsClient(_monitoredProcessId);
            using EventPipeSession session = diagnosticsClient.StartEventPipeSession(providers);
            EventPipeEventSource source = new EventPipeEventSource(session.EventStream);

            source.Dynamic.All += DynamicEventHandler;
            source.Clr.All += DotnetRuntimeEventHandler;

            try
            {
                source.Process();
            }
            catch (Exception e)
            {
                _applicationLogger.LogError(e, "Error encountered while processing events");
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel(false);
            }
        }

        private void DotnetRuntimeEventHandler(TraceEvent traceEvent)
        {
            if (traceEvent.ProviderName == DotnetRuntimeProviderName)
            {
                foreach (long eventId in EventIds)
                {
                    if ((eventId & (long)traceEvent.Keywords) != 0)
                    {
                        _eventHandlerByCode[eventId](traceEvent);
                    }
                }
            }
        }

        private void DynamicEventHandler(TraceEvent traceEvent)
        {
            if (_dynamicHandlers.TryGetValue(traceEvent.ProviderName, out TraceEventHandler handler))
            {
                handler(traceEvent);
            }
        }

        private delegate void TraceEventHandler(TraceEvent traceEvent);

        private void GcEventHandler(TraceEvent traceEvent)
        {
            switch (traceEvent.EventName)
            {
                case "GC/SuspendEEStart":
                    _garbageCollectionEventBuilder.OnSuspendStart(UtcTimestamp(traceEvent));
                    break;
                case "GC/SuspendEEStop":
                    _garbageCollectionEventBuilder.OnSuspendStop(UtcTimestamp(traceEvent));
                    break;
                case "GC/Start":
                    _garbageCollectionEventBuilder.OnGcStart(UtcTimestamp(traceEvent));
                    break;
                case "GC/Stop":
                    _garbageCollectionEventBuilder.OnGcStop(UtcTimestamp(traceEvent));
                    break;
                case "GC/RestartEEStart":
                    _garbageCollectionEventBuilder.OnUnsuspendStart(UtcTimestamp(traceEvent));
                    break;
                case "GC/RestartEEStop":
                    _garbageCollectionEventBuilder.OnUnsuspendStop(UtcTimestamp(traceEvent));
                    break;
                default:
                    // ignore other events
                    break;
            }
        }

        private void ContentionEventHandler(TraceEvent traceEvent)
        {
            if (traceEvent.EventName == "Contention/Stop")
            {
                double contentionDurationNs = (double)traceEvent.PayloadByName("DurationNs");
                _metricsPublisher.OnContention(_monitoredProcess.Label, UtcTimestamp(traceEvent),
                    (long)contentionDurationNs / 1000);
            }
        }

        private void ExceptionEventHandler(TraceEvent traceEvent)
        {
            if (traceEvent.EventName == "Exception/Start")
            {
                _exceptionLogger.OnExceptionStart(traceEvent.PayloadStringByName("ExceptionType"),
                    traceEvent.PayloadStringByName("ExceptionMessage"));
                _exceptionEventBuilders.TryAdd(traceEvent.ThreadID,
                    new ExceptionEventBuilder(_monitoredProcess.Label, _metricsPublisher));
                _exceptionEventBuilders[traceEvent.ThreadID].OnExceptionStart(UtcTimestamp(traceEvent));
            }
            else if (traceEvent.EventName == "Exception/Stop")
            {
                if (_exceptionEventBuilders.TryGetValue(traceEvent.ThreadID, out ExceptionEventBuilder builder))
                {
                    builder.OnExceptionStop(UtcTimestamp(traceEvent));
                }
            }
            else if (traceEvent.EventName == "ExceptionCatch/Start")
            {
                _exceptionLogger.OnExceptionCaught(traceEvent.PayloadStringByName("MethodName"));
            }
        }

        private static DateTime UtcTimestamp(TraceEvent traceEvent)
        {
            return traceEvent.TimeStamp.ToUniversalTime();
        }
    }
}