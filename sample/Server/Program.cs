using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using CoreWCF.Queue.Common.Configuration;

namespace Server
{
    public class Program
    {
        private static readonly string _queueName = "sample-sqs-queue";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // if needed, customize your aws credentials here,
            // otherwise it will default to searching ~\.aws
            var awsCredentials = new AWSOptions();

            builder
                .Services.AddDefaultAWSOptions(awsCredentials)
                .AddServiceModelServices()
                .AddQueueTransport()
                .AddSQSClient(_queueName);

            var app = builder.Build();

            var queueUrl = app.EnsureSqsQueue(
                _queueName,
                // optional callback for specifying how to create a queue
                // if it doesn't already exist
                createQueueRequest: new CreateQueueRequest().WithDeadLetterQueue()
            );

            app.UseServiceModel(services =>
            {
                services.AddService<LoggingService>();
                services.AddServiceEndpoint<LoggingService, ILoggingService>(new AwsSqsBinding(), queueUrl);
            });

            app.Run();
        }
    }
}
