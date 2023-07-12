using System.Text;
using DotMetrics.Codec.Metrics;
using Xunit;

namespace DotMetrics.Codec.Test.Metrics;

public class MappedMetricRepositoryTest : IDisposable
{
    private const string MetricOne = "metric-one";
    private const string MetricTwo = "metric-two";
    private const int RecordCount = 16;
    private readonly CapturingMetricValueReceiver _receiver = new CapturingMetricValueReceiver();
    private readonly MappedMetricRepository _repository;
    private readonly string _tmpDir;
    private readonly FileInfo _mappedFileInfo;

    public MappedMetricRepositoryTest()
    {
        _tmpDir = Directory.CreateTempSubdirectory().FullName;
        _mappedFileInfo = new FileInfo(Path.Combine(_tmpDir, "test-metric-repository.dm"));
        _repository = new MappedMetricRepository(_mappedFileInfo, RecordCount);
    }

    [Fact]
    public void ShouldReOpen()
    {
        _repository.GetOrCreate(MetricOne).SetValue(17);
        _repository.GetOrCreate(MetricTwo).SetValue(37);
        
        _repository.Dispose();
        MappedMetricRepository repository = new MappedMetricRepository(_mappedFileInfo, RecordCount);
        
        VerifyMetricValueInRepository(17, MetricOne, repository);
        VerifyMetricValueInRepository(37, MetricTwo, repository);
    }

    [Fact]
    public void ShouldGrowBackingFileWhenRecreated()
    {
        const int increasedRecordCount = 2 * RecordCount;
        MappedMetricRepository repository = new MappedMetricRepository(_mappedFileInfo, increasedRecordCount);
        
        VerifyRepositoryThrowsExceptionWhenFull(increasedRecordCount, repository);
    }

    [Fact]
    public void ShouldThrowExceptionIfIdentifierTooLong()
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < MappedMetricRepository.MaxIdentifierLength; i++)
        {
            builder.Append("x");
        }

        Assert.NotNull(_repository.GetOrCreate(builder.ToString()));
        builder.Append("x");
        Assert.Throws<Exception>(() => _repository.GetOrCreate(""));
    }

    [Fact]
    public void ShouldRequireNonEmptyIdentifier()
    {
        Assert.Throws<Exception>(() => _repository.GetOrCreate(""));
        Assert.Throws<Exception>(() => _repository.GetOrCreate(null));
    }

    [Fact]
    public void ShouldThrowExceptionWhenFull()
    {
        VerifyRepositoryThrowsExceptionWhenFull(RecordCount, _repository);
    }

    [Fact]
    public void ShouldAllowUseOfFullCapacity()
    {
        Dictionary<string, double> expectedValues = new Dictionary<string, double>();
        for (int i = 0; i < RecordCount; i++)
        {
            string identifier = $"metric-{i}";
            IMetricCounter metricCounter = _repository.GetOrCreate(identifier);
            int value = 37 * (i + 1);
            metricCounter.SetValue(value);
            expectedValues[identifier] = value;
        }

        foreach (KeyValuePair<string, double> expectedValue in expectedValues)
        {
            VerifyMetricValue(expectedValue.Value, expectedValue.Key);
        }
    }

    [Fact]
    public void ShouldCreateAndUpdateMetricCounterValues()
    {
        IMetricCounter metricOneCounter = _repository.GetOrCreate(MetricOne);
        IMetricCounter metricTwoCounter = _repository.GetOrCreate(MetricTwo);
        Random random = new Random();
        for (int i = 0; i < 1_000; i++)
        {
            double valueOne = random.NextDouble();
            double valueTwo = random.NextDouble();
            metricOneCounter.SetValue(valueOne);
            metricTwoCounter.SetValue(valueTwo);

            VerifyMetricValue(valueOne, MetricOne);
            VerifyMetricValue(valueTwo, MetricTwo);
        }
    }

    [Fact]
    public void ShouldReuseMetricCounterIfAlreadyPresent()
    {
        IMetricCounter instanceOne = _repository.GetOrCreate(MetricOne);
        IMetricCounter instanceTwo = _repository.GetOrCreate(MetricOne);

        Assert.NotSame(instanceOne, instanceTwo);

        instanceOne.SetValue(37);
        VerifyMetricValue(37, MetricOne);

        instanceTwo.SetValue(19);
        VerifyMetricValue(19, MetricOne);

        instanceOne.SetValue(1);
        VerifyMetricValue(1, MetricOne);
    }

    [Fact]
    public void ShouldCreateMultipleRepositoryInstances()
    {
        IMetricRepository repositoryTwo = new MappedMetricRepository(_mappedFileInfo, RecordCount);

        IMetricCounter instanceOne = _repository.GetOrCreate(MetricOne);

        IMetricCounter instanceTwo = repositoryTwo.GetOrCreate(MetricOne);
        IMetricCounter metricTwo = repositoryTwo.GetOrCreate(MetricTwo);

        instanceOne.SetValue(5);
        metricTwo.SetValue(17);

        VerifyMetricValueInRepository(5, MetricOne, _repository);
        VerifyMetricValueInRepository(5, MetricOne, repositoryTwo);

        VerifyMetricValueInRepository(17, MetricTwo, _repository);
        VerifyMetricValueInRepository(17, MetricTwo, repositoryTwo);

        instanceTwo.SetValue(11);

        VerifyMetricValueInRepository(11, MetricOne, _repository);
        VerifyMetricValueInRepository(11, MetricOne, repositoryTwo);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, true);
    }

    private static void VerifyRepositoryThrowsExceptionWhenFull(int maxRecordCount, IMetricRepository repository)
    {
        for (int i = 0; i < maxRecordCount; i++)
        {
            string identifier = $"metric-{i}";
            Assert.NotNull(repository.GetOrCreate(identifier));
        }

        Assert.Throws<Exception>(() => repository.GetOrCreate("other"));
        Assert.Throws<Exception>(() => repository.GetOrCreate("other"));
    }

    private void VerifyMetricValue(double value, string metricName)
    {
        VerifyMetricValueInRepository(value, metricName, _repository);
    }

    private void VerifyMetricValueInRepository(double value, string metricName, IMetricRepository repository)
    {
        _receiver.CapturedMetrics.Clear();
        repository.Read(_receiver);
        Assert.Equal(value, _receiver.CapturedMetrics[metricName].Value);
    }
}