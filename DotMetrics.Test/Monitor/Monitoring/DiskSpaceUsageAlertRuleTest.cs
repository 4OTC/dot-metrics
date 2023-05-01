using DotMetrics.Monitor.Monitoring;
using DotMetrics.Monitor.Monitoring.DiskSpace;
using Moq;
using Xunit;

namespace DotMetrics.Test.Monitor.Monitoring;

public class DiskSpaceUsageAlertRuleTest
{
    private const int UtilisationThresholdPercentage = 42;
    private readonly DiskSpaceUsageAlertRule _rule;

    private readonly Mock<IDiskUsage> _diskUsageMock = new Mock<IDiskUsage>();

    public DiskSpaceUsageAlertRuleTest()
    {
        _rule = new DiskSpaceUsageAlertRule("/", UtilisationThresholdPercentage, (_) => _diskUsageMock.Object);
    }

    [Fact]
    public void ShouldBeTriggeredOnceWhenUtilisationOverThreshold()
    {
        SetUtilisation(UtilisationThresholdPercentage - 1);
        Assert.False(_rule.IsTriggered());

        SetUtilisation(UtilisationThresholdPercentage);
        Assert.True(_rule.IsTriggered());
        Assert.False(_rule.IsTriggered());

        SetUtilisation(UtilisationThresholdPercentage - 1);
        Assert.False(_rule.IsTriggered());

        SetUtilisation(UtilisationThresholdPercentage + 1);
        Assert.True(_rule.IsTriggered());
        Assert.False(_rule.IsTriggered());

        SetUtilisation(UtilisationThresholdPercentage - 1);
        Assert.False(_rule.IsTriggered());
    }

    private void SetUtilisation(int currentUtilisation)
    {
        _diskUsageMock.Setup(x => x.GetUtilisationPercentage()).Returns(currentUtilisation);
    }
}