using System;
using DotMetrics.Monitor.Event;
using DotMetrics.Monitor.Publish;
using Moq;
using Xunit;

namespace DotMetrics.Test.Monitor.Event
{
    public class GarbageCollectionEventBuilderTest
    {
        private const string ServiceName = "first-service";
        private readonly IncrementingClock _clock = new();
        private readonly Mock<IMetricsPublisher> _publisherMock = new();
        private readonly GarbageCollectionEventBuilder _builder;

        public GarbageCollectionEventBuilderTest()
        {
            _builder = new(ServiceName, _publisherMock.Object);
        }

        [Fact]
        public void ShouldPublishGarbageCollectionSummary()
        {
            _builder.OnSuspendStart(_clock.SampleTime(0));
            _builder.OnSuspendStop(_clock.SampleTime(17));
            _builder.OnGcStart(_clock.SampleTime(1));
            _builder.OnGcStop(_clock.SampleTime(13));
            _builder.OnUnsuspendStart(_clock.SampleTime(3));
            _builder.OnUnsuspendStop(_clock.SampleTime(5));

            _publisherMock.Verify(x => x.OnGarbageCollection(
                ServiceName, _clock.StartTime, 17_000, 13_000, 5_000, 39_000
            ));
            _publisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void ShouldIgnoreEventsUntilSuspendStartIsReceived()
        {
            _builder.OnSuspendStop(_clock.SampleTime(317));
            _builder.OnGcStart(_clock.SampleTime(31));
            _builder.OnGcStop(_clock.SampleTime(313));
            _builder.OnUnsuspendStart(_clock.SampleTime(33));
            _builder.OnUnsuspendStop(_clock.SampleTime(35));

            DateTime suspendStartTime = _clock.DateTime;
            _builder.OnSuspendStart(_clock.SampleTime(0));
            _builder.OnSuspendStop(_clock.SampleTime(17));
            _builder.OnGcStart(_clock.SampleTime(1));
            _builder.OnGcStop(_clock.SampleTime(13));
            _builder.OnUnsuspendStart(_clock.SampleTime(3));
            _builder.OnUnsuspendStop(_clock.SampleTime(5));
            _builder.OnUnsuspendStop(_clock.SampleTime(35));

            _publisherMock.Verify(x => x.OnGarbageCollection(
                ServiceName, suspendStartTime, 17_000, 13_000, 5_000, 39_000
            ));
            _publisherMock.VerifyNoOtherCalls();
        }
    }
}