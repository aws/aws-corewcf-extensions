using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using AWS.Extensions.IntegrationTests.SQS.TestService;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static AWS.Extensions.IntegrationTests.SQS.ClientAndServerFixture;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AWS.Extensions.IntegrationTests.SQS;

[CollectionDefinition("ClientAndServer collection")]
public class ClientAndServerCollectionFixture : ICollectionFixture<ClientAndServerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public partial class ClientAndServerFixture : IDisposable
{
    private ChannelFactory<ILoggingService> _factory;
    
    public const string QueueWithDefaultSettings = "CoreWCFExtensionsDefaultSettingsQueue";
    public const string FifoQueueName = "CoreWCFExtensionsTest.fifo";

    public IWebHost Host { get; private set; }
    public ILoggingService Channel { get; private set; }
    public IAmazonSQS SqsClient { get; private set; }

    public void Start(ITestOutputHelper testOutputHelper)
    {
        var settingsJson = File.ReadAllText(Path.Combine("SQS", "appsettings.test.json"));

        var settings = JsonSerializer.Deserialize<Settings>(settingsJson)!;

        SqsClient = new AmazonSQSClient(
            new BasicAWSCredentials(settings.AWS.AWS_ACCESS_KEY_ID, settings.AWS.AWS_SECRET_ACCESS_KEY),
            RegionEndpoint.GetBySystemName(settings.AWS.AWS_REGION));

        Host =
            ServiceHelper
                .CreateServiceHost<Startup>(services => 
                    services
                        .AddSingleton(settings)
                        .AddLogging(builder => builder.AddProvider(new XUnitLoggingProvider(testOutputHelper))))
                .Build();

        Host.Start();

        CreateAndOpenClientChannel();
        //EnsureQueueIsEmpty();
    }

    public void Dispose()
    {
        if (Host != null)
        {
            Host.Dispose();
        }
    }

    //private void EnsureQueueIsEmpty()
    //{
        //var response = SqsClient.PurgeQueueAsync(QueueUrl).Result;
        //response.Validate();
    //}

    private void CreateAndOpenClientChannel()
    {
        var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(SqsClient, QueueWithDefaultSettings);
        var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
        _factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
        Channel = _factory.CreateChannel();
        ((System.ServiceModel.Channels.IChannel)Channel).Open();
    }

    private class Startup
    {
        private readonly Settings _settings;

        public Startup(Settings settings)
        {
            _settings = settings;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices();
            services.AddSingleton<ILogger>(NullLogger.Instance);
#if DEBUG
            services.AddLogging(builder =>
            {
                builder.AddConsole();
            });
#endif

            var bindAwsOptionsToSettings = new Action<AWSOptions>(opts =>
            {
                opts.Credentials = new BasicAWSCredentials(_settings.AWS.AWS_ACCESS_KEY_ID, _settings.AWS.AWS_SECRET_ACCESS_KEY);
                opts.Region = RegionEndpoint.GetBySystemName(_settings.AWS.AWS_REGION);
            });

            
            services.AddSQSClient(
                QueueWithDefaultSettings,
                bindAwsOptionsToSettings,
                (sqsClient, awsOptions, queueName) =>
                {
                    sqsClient.EnsureSQSQueue(awsOptions, new CreateQueueRequest(queueName).SetDefaultValues());
                }
            );

            services.AddSQSClient(
                FifoQueueName,
                bindAwsOptionsToSettings,
                (sqsClient, awsOptions, queueName) =>
                {
                    sqsClient
                        .EnsureSQSQueue(
                            awsOptions,
                            new CreateQueueRequest(queueName)
                                .SetDefaultValues()
                                .WithFIFO()
                                .WithManagedServerSideEncryption()
                        )
                        .WithBasicPolicy(queueName);
                }
            );

            services.AddAWSService<IAmazonSimpleNotificationService>();
            services.AddQueueTransport();
        }

        public void Configure(IApplicationBuilder app)
        {
            var queueName = QueueWithDefaultSettings;

            var sqsClient = app.ApplicationServices.GetServices<NamedSQSClient>()
                .FirstOrDefault(x => x.QueueName == queueName)?.SQSClient;

            if (null == sqsClient)
                throw new Exception($"Failed to find SqsClient for Queue: [{queueName}]");

            var queueUrl = sqsClient.GetQueueUrlAsync(queueName).Result.QueueUrl;

            var successTopicArn = _settings.AWS.SUCCESS_TOPIC_ARN; 
            var failureTopicArn = _settings.AWS.FAILURE_TOPIC_ARN;

            app.UseServiceModel(services =>
            {
                services.AddService<LoggingService>();
                services.AddServiceEndpoint<LoggingService, ILoggingService>(
                    new AWS.CoreWCF.Extensions.SQS.Channels.AwsSqsBinding
                    {
                        QueueName = queueName,
                        DispatchCallbacksCollection =
                            DispatchCallbacksCollectionFactory.GetDefaultCallbacksCollectionWithSns(
                                successTopicArn,
                                failureTopicArn
                            )
                    },
                    queueUrl
                );
            });
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Settings
    {
        public AWSSettings AWS { get; set; } = new();

        public class AWSSettings
        {
            //public string? PROFILE { get; set; }
            public string? AWS_ACCESS_KEY_ID { get; set; }
            public string? AWS_SECRET_ACCESS_KEY { get; set; }
            public string? AWS_REGION { get; set; } = "us-west-2";
            public string? FAILURE_TOPIC_ARN { get; set; }
            public string? SUCCESS_TOPIC_ARN { get; set; }
            public string? TEST_QUEUE_NAME { get; set; }
        }
    }
}
