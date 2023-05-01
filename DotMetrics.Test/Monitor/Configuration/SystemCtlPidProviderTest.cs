using System;
using System.Collections.Generic;
using DotMetrics.Monitor.Configuration;
using DotMetrics.Monitor.ProcessUtil;
using Moq;
using Xunit;

namespace DotMetrics.Test.Monitor.Configuration
{
    public class SystemCtlPidProviderTest
    {
        private const string FirstApplicationService = "first-application.service";
        private const string SecondApplicationService = "second-application.service";
        private readonly Mock<IProcessExecutor> _executorMock = new();
        private readonly SystemCtlPidProvider _pidProvider;

        public SystemCtlPidProviderTest()
        {
            _pidProvider =
                new SystemCtlPidProvider(new[] { FirstApplicationService, SecondApplicationService },
                    _executorMock.Object);
        }

        [Fact]
        public void ShouldParsePidFromSystemCtlResponse()
        {
            SetUpValidResponse();
            var monitoredPids = _pidProvider.GetMonitoredProcesses();

            List<string> stdOut = new();
            List<string> stdErr = new();
            Exception exception = null;

            Assert.Equal(new ProcessInfo[] { new(42, FirstApplicationService), new(777, SecondApplicationService) },
                monitoredPids);
            _executorMock.Verify(x => x.Execute("/bin/systemctl", new[] { "status", FirstApplicationService },
                out stdOut, out stdErr, out exception));
            _executorMock.Verify(x => x.Execute("/bin/systemctl", new[] { "status", SecondApplicationService },
                out stdOut, out stdErr, out exception));
        }

        [Fact]
        public void ShouldThrowExceptionWhenOutputDoesNotContainMainPidLine()
        {
            SetUpValidFirstServiceResult();

            List<string> secondServiceStdOut = new();
            secondServiceStdOut.Add(
                "     Active: active (running) since Thu 2021-05-20 20:08:37 BST; 2 weeks 3 days ago");
            secondServiceStdOut.Add("      Tasks: 36");
            SetUpExecutionResult(SecondApplicationService, secondServiceStdOut, new(), null, true);
            Assert.Throws<Exception>(() => _pidProvider.GetMonitoredProcesses());
        }

        [Fact]
        public void ShouldThrowExceptionWhenOutputContainsInvalidPid()
        {
            SetUpValidFirstServiceResult();

            List<string> secondServiceStdOut = new();
            secondServiceStdOut.Add(
                "     Active: active (running) since Thu 2021-05-20 20:08:37 BST; 2 weeks 3 days ago");
            secondServiceStdOut.Add("   Main PID: NOT_A_VALID_PID (second-application.service)");
            secondServiceStdOut.Add("      Tasks: 36");
            SetUpExecutionResult(SecondApplicationService, secondServiceStdOut, new(), null, true);
            Assert.Throws<Exception>(() => _pidProvider.GetMonitoredProcesses());
        }

        [Fact]
        public void ShouldThrowExceptionWhenSystemctlExecutionFails()
        {
            SetUpValidFirstServiceResult();

            List<string> secondServiceStdOut = new();
            secondServiceStdOut.Add(
                "     Active: active (running) since Thu 2021-05-20 20:08:37 BST; 2 weeks 3 days ago");
            secondServiceStdOut.Add("   Main PID: 777 (second-application.service)");
            secondServiceStdOut.Add("      Tasks: 36");
            SetUpExecutionResult(SecondApplicationService, secondServiceStdOut, new(), null, false);
            Assert.Throws<Exception>(() => _pidProvider.GetMonitoredProcesses());
        }

        private void SetUpValidResponse()
        {
            SetUpValidFirstServiceResult();
            List<string> secondServiceStdOut = new();
            secondServiceStdOut.Add(
                "     Active: active (running) since Thu 2021-05-20 20:08:37 BST; 2 weeks 3 days ago");
            secondServiceStdOut.Add("   Main PID: 777 (second-application.service)");
            secondServiceStdOut.Add("      Tasks: 36");
            SetUpExecutionResult(SecondApplicationService, secondServiceStdOut, new(), null, true);
        }

        private void SetUpValidFirstServiceResult()
        {
            List<string> firstServiceStdOut = new();
            firstServiceStdOut.Add(
                "     Active: active (running) since Thu 2021-05-20 20:08:37 BST; 2 weeks 3 days ago");
            firstServiceStdOut.Add("   Main PID: 42 (first-application.service)");
            firstServiceStdOut.Add("      Tasks: 36");
            SetUpExecutionResult(FirstApplicationService, firstServiceStdOut, new(), null, true);
        }

        private void SetUpExecutionResult(string serviceName, List<string> stdOut, List<string> stdErr,
            Exception exception,
            bool executionSuccess)
        {
            _executorMock.Setup(x => x.Execute(
                It.IsAny<string>(),
                It.Is<string[]>(input => input[1] == serviceName),
                out stdOut, out stdErr,
                out exception)).Returns(executionSuccess);
        }
    }
}