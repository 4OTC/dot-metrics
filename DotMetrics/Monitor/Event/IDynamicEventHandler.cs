using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;

namespace DotMetrics.Monitor.Event
{
    public interface IDynamicEventHandler
    {
        EventPipeProvider GetProvider();

        void HandleEvent(TraceEvent traceEvent);
    }
}