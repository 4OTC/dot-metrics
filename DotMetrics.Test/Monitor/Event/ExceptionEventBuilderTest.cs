using System;
using DotMetrics.Monitor.Event;
using DotMetrics.Monitor.Publish;
using Moq;
using Xunit;

namespace DotMetrics.Test.Monitor.Event
{
    public class ExceptionEventBuilderTest
    {
        private readonly Mock<IMetricsPublisher> _metricsPublisherMock = new();
        private readonly ExceptionEventBuilder _builder;
        private const string ServiceName = "first-service";
        private readonly IncrementingClock _clock = new();

        public ExceptionEventBuilderTest()
        {
            _builder = new ExceptionEventBuilder(ServiceName, _metricsPublisherMock.Object);
        }

        [Fact]
        public void ShouldPublishExceptionEvent()
        {
            _builder.OnExceptionStart(_clock.SampleTime(3765));
            DateTime startTime = _clock.DateTime;
            _builder.OnExceptionStop(_clock.SampleTime(7654));

            _metricsPublisherMock.Verify(x => x.OnException(ServiceName, startTime, 7_654_000));
            _metricsPublisherMock.VerifyNoOtherCalls();
        }

        [Fact]
        public void ShouldIgnoreExceptionStopEventsBeforeExceptionStart()
        {
            _builder.OnExceptionStop(_clock.SampleTime(42));
            _builder.OnExceptionStop(_clock.SampleTime(37));
            _builder.OnExceptionStart(_clock.SampleTime(100));
            DateTime startTime = _clock.DateTime;
            _builder.OnExceptionStop(_clock.SampleTime(17));
            _builder.OnExceptionStop(_clock.SampleTime(337));

            _metricsPublisherMock.Verify(x => x.OnException(ServiceName, startTime, 17_000));
            _metricsPublisherMock.VerifyNoOtherCalls();
        }
    }
}