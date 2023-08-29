using System.Threading.Tasks;

namespace DotMetrics.Monitor.Alerting;

public interface IAlertSender
{
    Task SendAsync(string alertMessage);
}