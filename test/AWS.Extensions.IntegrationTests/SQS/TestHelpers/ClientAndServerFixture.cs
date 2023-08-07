using System.ServiceModel;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using AWS.Extensions.IntegrationTests.Common;
using AWS.Extensions.IntegrationTests.SQS.TestService;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AWS.Extensions.IntegrationTests.SQS.TestHelpers;

[CollectionDefinition("ClientAndServer collection")]
public class ClientAndServerCollectionFixture : ICollectionFixture<ClientAndServerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class ClientAndServerFixture : IDisposable
{
    private ChannelFactory<ILoggingService> _factory;

    public const string QueueWithDefaultSettings = "CoreWCFExtensionsDefaultSettingsQueue";
    public const string FifoQueueName = "CoreWCFExtensionsTest.fifo";

    public IWebHost? Host { get; private set; }
    public ILoggingService? Channel { get; private set; }
    public IAmazonSQS? SqsClient { get; private set; }
    public string QueueName { get; private set; }

    public IntegrationTestAWSOptionsBuilder AWSOptionsBuilder { get; private set; }
    
    public void Start(
        ITestOutputHelper testOutputHelper,
        string queueName = QueueWithDefaultSettings,
        IDispatchCallbacksCollection? dispatchCallbacks = null)
    {
        QueueName = queueName;

        var settingsJson = File.ReadAllText(Path.Combine("SQS", "appsettings.test.json"));

        var settings = JsonSerializer.Deserialize<Settings>(settingsJson)!;

        AWSOptionsBuilder = new IntegrationTestAWSOptionsBuilder(settings);

        SqsClient = new AmazonSQSClient(
            new BasicAWSCredentials(settings.AWS.AWS_ACCESS_KEY_ID, settings.AWS.AWS_SECRET_ACCESS_KEY),
            RegionEndpoint.GetBySystemName(settings.AWS.AWS_REGION));

        dispatchCallbacks ??=
            DispatchCallbacksCollectionFactory.GetDefaultCallbacksCollectionWithSns(
                settings.AWS.SUCCESS_TOPIC_ARN ?? "",
                settings.AWS.FAILURE_TOPIC_ARN ?? ""
            );

        Host =
            ServiceHelper
                .CreateServiceHost<Startup>(services =>
                    services
                        .AddSingleton(AWSOptionsBuilder)
                        .AddSingleton(new DefaultQueueNameProvider{ QueueName = queueName})
                        .AddSingleton<IDispatchCallbacksCollection>(dispatchCallbacks)
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

    private void CreateAndOpenClientChannel()
    {
        var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(SqsClient, QueueName);
        var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
        _factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
        Channel = _factory.CreateChannel();
        ((System.ServiceModel.Channels.IChannel) Channel).Open();
    }

    private class Startup
    {
        private readonly IntegrationTestAWSOptionsBuilder _awsOptionsBuilder;
        private readonly DefaultQueueNameProvider _defaultQueueNameProvider;
        private readonly IDispatchCallbacksCollection _dispatchCallbacksCollection;

        public Startup(
            IntegrationTestAWSOptionsBuilder awsOptionsBuilder,
            DefaultQueueNameProvider defaultQueueNameProvider,
            IDispatchCallbacksCollection dispatchCallbacksCollection)
        {
            _awsOptionsBuilder = awsOptionsBuilder;
            _defaultQueueNameProvider = defaultQueueNameProvider;
            _dispatchCallbacksCollection = dispatchCallbacksCollection;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddServiceModelServices();
            services.AddSingleton<ILogger>(NullLogger.Instance);
#if DEBUG
            services.AddLogging(builder => { builder.AddConsole(); });
#endif

            services.AddSQSClient(
                _defaultQueueNameProvider.QueueName,
                _awsOptionsBuilder.Populate,
                (sqsClient, awsOptions, queueName) =>
                {
                    sqsClient.EnsureSQSQueue(awsOptions, new CreateQueueRequest(queueName).SetDefaultValues()).Wait();
                }
            );

            services.AddSQSClient(
                FifoQueueName,
                _awsOptionsBuilder.Populate,
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
                        .Result
                        .WithBasicPolicy(queueName);
                }
            );

            services.AddAWSService<IAmazonSimpleNotificationService>();
            services.AddQueueTransport();
        }

        public void Configure(IApplicationBuilder app)
        {
            var queueName = _defaultQueueNameProvider.QueueName;

            var sqsClient = app.ApplicationServices.GetServices<NamedSQSClient>()
                .FirstOrDefault(x => x.QueueName == queueName)?.SQSClient;

            if (null == sqsClient)
                throw new Exception($"Failed to find SqsClient for Queue: [{queueName}]");

            var queueUrl = sqsClient.GetQueueUrlAsync(queueName).Result.QueueUrl;

            app.UseServiceModel(services =>
            {
                services.AddService<LoggingService>();
                services.AddServiceEndpoint<LoggingService, ILoggingService>(
                    new AWS.CoreWCF.Extensions.SQS.Channels.AwsSqsBinding
                    {
                        QueueName = queueName,
                        DispatchCallbacksCollection = _dispatchCallbacksCollection
                    },
                    queueUrl
                );
            });
        }
    }

    public class IntegrationTestAWSOptionsBuilder
    {
        private readonly Settings _settings;

        public IntegrationTestAWSOptionsBuilder(Settings settings)
        {
            _settings = settings;
        }

        public void Populate(AWSOptions awsOptions)
        {
            awsOptions.Credentials = new BasicAWSCredentials(_settings.AWS.AWS_ACCESS_KEY_ID, _settings.AWS.AWS_SECRET_ACCESS_KEY);
            awsOptions.Region = RegionEndpoint.GetBySystemName(_settings.AWS.AWS_REGION);
        }
    }

    public class DefaultQueueNameProvider
    {
        public string QueueName { get; set; }
    }
}
