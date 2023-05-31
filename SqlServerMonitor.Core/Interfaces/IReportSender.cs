namespace SqlServerMonitor.Core.Interfaces;

public interface IReportSender
{
    void Send(string report);
}