using System.Collections.Concurrent;
using System.Diagnostics;
using System.Xml.Linq;
using CoreWCF;

internal static class Constants
{
    public const string NS = "http://tempuri.org/";
    public const string LOGGINGSERVICE_NAME = nameof(ILoggingService);
    public const string OPERATION_BASE = NS + LOGGINGSERVICE_NAME + "/";
}

[System.ServiceModel.ServiceContract(Namespace = Constants.NS, Name = Constants.LOGGINGSERVICE_NAME)]
[ServiceContract(Namespace = Constants.NS, Name = Constants.LOGGINGSERVICE_NAME)]
public interface ILoggingService
{
    [System.ServiceModel.OperationContract(
        Name = "LogMessage",
        Action = Constants.OPERATION_BASE + "LogMessage",
        IsOneWay = true
    )]
    [OperationContract(Name = "LogMessage", Action = Constants.OPERATION_BASE + "LogMessage", IsOneWay = true)]
    public void LogMessage(string toLog);

    [System.ServiceModel.OperationContract(
        Name = "CauseFailure",
        Action = Constants.OPERATION_BASE + "CauseFailure",
        IsOneWay = true
    )]
    [OperationContract(Name = "CauseFailure", Action = Constants.OPERATION_BASE + "CauseFailure", IsOneWay = true)]
    void CauseFailure();
}

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class LoggingService : ILoggingService
{
    public void LogMessage(string toLog)
    {
        Console.WriteLine("Received " + toLog);
        Debug.WriteLine("Received " + toLog, category: "LoggingService");
    }

    public void CauseFailure()
    {
        throw new Exception("Trigger Failure");
    }
}
