namespace DotMetrics.Codec.Metrics;

public interface IMetricRepository
{
    IMetricCounter GetOrCreate(string identifier);
    void Read(IMetricValueReceiver metricValueReceiver);
}