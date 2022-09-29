// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using CoreWCF;

namespace AWS.CoreWCF.Server.SQS.Tests.TestService
{
    internal static class Constants
    {
        public const string NS = "http://tempuri.org/";
        public const string LOGGINGSERVICE_NAME = nameof(ILoggingService);
        public const string OPERATION_BASE = NS + LOGGINGSERVICE_NAME + "/";
    }

    [ServiceContract(Namespace = Constants.NS, Name = Constants.LOGGINGSERVICE_NAME)]
    public interface ILoggingService
    {
        [OperationContract(
            Name = "LogMessage",
            Action = Constants.OPERATION_BASE + "LogMessage",
            IsOneWay = true)]
        public void LogMessage(string toLog);
    }

    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class LoggingService : ILoggingService
    {
        public static readonly ConcurrentDictionary<string, ManualResetEventSlim> LogResults = new();

        internal static string InitializeTestCase(string testCaseName)
        {
            LogResults[testCaseName] = new ManualResetEventSlim(false);
            return testCaseName;
        }

        public void LogMessage(string toLog)
        {
            LogResults[toLog].Set();
        }
    }
}
