using SqlServerMonitor.Core.Interfaces;

namespace SqlServerMonitor.Core.Senders;

public class ConsoleWriterSender: IReportSender
{
    public void Send(string report)
    {
        Console.WriteLine(report);
    }
}