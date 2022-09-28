// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CoreWCF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWS.CoreWCF.Server.SQS.Tests.TestService
{
    internal static class Constants
    {
        public const string NS = "http://tempuri.org/";
        public const string LOGGINGSERVICE_NAME = nameof(ILoggingService);
        public const string OPERATION_BASE = NS + LOGGINGSERVICE_NAME + "/";
    }

    [ServiceContract(Namespace = Constants.NS, Name = Constants.LOGGINGSERVICE_NAME)]
    //[ServiceContract]
    public interface ILoggingService
    {
        [OperationContract(
            Name = "LogMessage",
            Action = Constants.OPERATION_BASE + "LogMessage",
            IsOneWay = true)]
        //[OperationContract(IsOneWay = true)]
        public void LogMessage(string toLog);
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class LoggingService : ILoggingService
    {
        private IServiceProvider _services;
        public ManualResetEventSlim ManualResetEvent { get; }

        public LoggingService(IServiceProvider services)
        {
            _services = services;
            ManualResetEvent = new ManualResetEventSlim(false);
        }

        public void LogMessage(string toLog)
        {
            var logger = _services.GetRequiredService<ILogger>();
            logger.LogInformation(toLog);
            ManualResetEvent.Set();
        }
    }
}
