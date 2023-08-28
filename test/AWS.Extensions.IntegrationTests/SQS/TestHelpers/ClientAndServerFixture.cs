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
using AWS.Extensions.IntegrationTests.Common;
using AWS.Extensions.IntegrationTests.SQS.TestService;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common.Configuration;
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

[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
public class ClientAndServerFixture : IDisposable
{
    private ChannelFactory<ILoggingService>? _factory;

    public const string QueueWithDefaultSettings = "CoreWCFExtensionsDefaultSettingsQueue";
    public const string FifoQueueName = "CoreWCFExtensionsTest.fifo";
    public const string SnsNotificationSuccessQueue = "CoreWCF-SNSSuccessQueue";

    public IWebHost? Host { get; private set; }
    public ILoggingService? Channel { get; private set; }
    public IAmazonSQS? SqsClient { get; private set; }
    public string? QueueName { get; private set; }

    public Settings? Settings { get; private set; }

    public IntegrationTestAWSOptionsBuilder? AWSOptionsBuilder { get; private set; }

    public void Start(
        ITestOutputHelper testOutputHelper,
        string queueName = QueueWithDefaultSettings,
        IDispatchCallbacksCollection? dispatchCallbacks = null
    )
    {
        QueueName = queueName;

        var settingsJson = File.ReadAllText(Path.Combine("SQS", "appsettings.test.json"));

        Settings = JsonSerializer.Deserialize<Settings>(settingsJson)!;

        AWSOptionsBuilder = new IntegrationTestAWSOptionsBuilder(Settings);

        SqsClient = new AmazonSQSClient(
            new BasicAWSCredentials(Settings.AWS.AWS_ACCESS_KEY_ID, Settings.AWS.AWS_SECRET_ACCESS_KEY),
            RegionEndpoint.GetBySystemName(Settings.AWS.AWS_REGION)
        );

        dispatchCallbacks ??= DispatchCallbacksCollectionFactory.GetDefaultCallbacksCollectionWithSns(
            Settings.AWS.SUCCESS_TOPIC_ARN ?? "",
            Settings.AWS.FAILURE_TOPIC_ARN ?? ""
        );

        Host = ServiceHelper
            .CreateServiceHost(
                configureServices: services =>
                    services
                        .AddDefaultAWSOptions(AWSOptionsBuilder.Build())
                        .AddSingleton<ILogger>(NullLogger.Instance)
                        .AddAWSService<IAmazonSimpleNotificationService>()
                        .AddServiceModelServices()
                        .AddQueueTransport()
                        .AddSQSClient(QueueName)
                        .AddSQSClient(FifoQueueName),
                configure: app =>
                {
                    // OPTION B:
                    // 1.find the NamedSqsClient associated with QueueName
                    // 2. use the SQSClient to make sdk calls
                    // tweak: remove NamedSQSClient.QueueUrl to make it clear that queueUrl should
                    // not be resolved eagerly.
                    var queueUrl = app.EnsureSqsQueue(QueueName);
                    var fifoQueueUrl = app.EnsureSqsQueue(FifoQueueName);

                    app.UseServiceModel(services =>
                    {
                        services.AddService<LoggingService>();
                        services.AddServiceEndpoint<LoggingService, ILoggingService>(
                            new AwsSqsBinding
                            {
                                QueueName = QueueName,
                                DispatchCallbacksCollection = dispatchCallbacks
                            },
                            queueUrl
                        );
                        services.AddServiceEndpoint<LoggingService, ILoggingService>(
                            new AwsSqsBinding
                            {
                                QueueName = FifoQueueName,
                                DispatchCallbacksCollection = dispatchCallbacks
                            },
                            fifoQueueUrl
                        );
                    });
                },
                testOutputHelper: testOutputHelper
            )
            .Build();

        Host.Start();

        // Start Client
        var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(SqsClient, QueueName);
        var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
        _factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
        Channel = _factory.CreateChannel();
        ((System.ServiceModel.Channels.IChannel)Channel).Open();
    }

    public void Dispose()
    {
        if (Host != null)
        {
            Host.Dispose();
        }

        if (Channel != null)
        {
            ((System.ServiceModel.Channels.IChannel)Channel).Close();
        }
    }

    public class IntegrationTestAWSOptionsBuilder
    {
        private readonly Settings _settings;

        public IntegrationTestAWSOptionsBuilder(Settings settings)
        {
            _settings = settings;
        }

        public AWSOptions Build()
        {
            var options = new AWSOptions();
            Populate(options);
            return options;
        }

        public void Populate(AWSOptions awsOptions)
        {
            awsOptions.Credentials = new BasicAWSCredentials(
                _settings.AWS.AWS_ACCESS_KEY_ID,
                _settings.AWS.AWS_SECRET_ACCESS_KEY
            );
            awsOptions.Region = RegionEndpoint.GetBySystemName(_settings.AWS.AWS_REGION);
        }
    }
}
