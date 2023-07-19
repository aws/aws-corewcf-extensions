using System.ServiceModel;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Demo;

namespace AWS.CoreWCF.Server.SQS.Tests;

public class Demo
{
    private static IWebHost _host;
    private static ChannelFactory<ILoggingService> _factory;
    public static ILoggingService Channel { get; private set; }

    public static void Main()
    {
        // Mimic environment setup
        Console.WriteLine("Setting up credentials...");
        EnvironmentCollectionFixture.InitializeEnvironment();
        var sqsClient = EnvironmentCollectionFixture.GetSqsClient();
        var queueUrl = EnvironmentCollectionFixture.QueueUrl;

        // Set up the WCF Client
        Console.WriteLine("Setting up the client...");
        var sqsBinding = new WCF.Extensions.SQS.AwsSqsBinding(sqsClient, queueUrl);
        var endpointAddress = new EndpointAddress(new Uri(queueUrl));
        _factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
        Channel = _factory.CreateChannel();
        ((System.ServiceModel.Channels.IChannel)Channel).Open();

        // Set up the CoreWCF Server
        Console.WriteLine("Setting up the server...");
        _host = ServiceHelper.CreateServiceHost<Startup>().Build();
        _host.Start();

        // Simple loop to send a message to the queue every second
        Console.WriteLine("Running the application...");
        var messageId = 1;
        while (true)
        {
            Channel.LogMessage($"Sending log message with ID: {messageId++}");
            Thread.Sleep(1000);
        }
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable basic logging
            services.AddSingleton<ILogger>(NullLogger.Instance);
            services.AddLogging(builder =>
            {
                builder.AddConsole();
            });

            // Enable CoreWCF services
            services.AddServiceModelServices();

            // Setup AWS credentials + services
            services.AddDefaultAWSOptions(
                new AWSOptions { Credentials = EnvironmentCollectionFixture.GetCredentials() }
            );
            services.AddAWSService<IAmazonSQS>();
            services.AddAWSService<IAmazonSimpleNotificationService>();

            // Enable CoreWCF queueing
            services.AddQueueTransport();
        }

        public void Configure(IApplicationBuilder app)
        {
            var queueUrl = EnvironmentCollectionFixture.GetTestQueueUrl() ?? string.Empty;
            var concurrencyLevel = 1;
            var successTopicArn = EnvironmentCollectionFixture.SuccessTopicArn;
            var failureTopicArn = EnvironmentCollectionFixture.FailureTopicArn;
            app.UseServiceModel(services =>
            {
                services.AddService<LoggingService>();
                services.AddServiceEndpoint<LoggingService, ILoggingService>(
                    new AwsSqsBinding(
                        queueUrl,
                        concurrencyLevel,
                        DispatchCallbacksCollectionFactory.GetDefaultCallbacksCollectionWithSns(
                            successTopicArn,
                            failureTopicArn
                        )
                    ),
                    "/BasicSqsService/ILoggingService.svc"
                );
            });
        }
    }
}
