using System.Collections.Concurrent;
using CoreWCF;

namespace AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;

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
}

[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class LoggingService : ILoggingService
{
    public static readonly ConcurrentBag<string> LogResults = new();

    public void LogMessage(string toLog)
    {
        LogResults.Add(toLog);
    }
}
