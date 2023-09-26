using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.IdentityManagement;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
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
using Microsoft.Extensions.Configuration;
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

    public const string SuccessTopicName = "CoreWCF-Success";
    public const string FailureTopicName = "CoreWCF-Failure";

    public IWebHost? Host { get; private set; }
    public ILoggingService? Channel { get; private set; }
    public IAmazonSQS? SqsClient { get; private set; }
    public IAmazonSecurityTokenService? StsClient { get; set; }
    public IAmazonIdentityManagementService? IamClient { get; set; }
    public string? QueueName { get; private set; }

    public Settings? Settings { get; private set; }

    public void Start(
        ITestOutputHelper testOutputHelper,
        string queueName,
        CreateQueueRequest? createQueue = null,
        IDispatchCallbacksCollection? dispatchCallbacks = null
    )
    {
        QueueName = queueName;

        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine("SQS", "appsettings.test.json"))
            .AddEnvironmentVariables()
            .Build();

        Settings = config.Get<Settings>();

        var defaultAwsOptions = !string.IsNullOrEmpty(Settings?.AWS?.AWS_ACCESS_KEY_ID)
            ? new AWSOptions
            {
                Credentials = new BasicAWSCredentials(
                    Settings.AWS.AWS_ACCESS_KEY_ID,
                    Settings.AWS.AWS_SECRET_ACCESS_KEY
                ),
                Region = RegionEndpoint.GetBySystemName(Settings.AWS.AWS_REGION)
            }
            : config.GetAWSOptions();

        // bootstrap helper aws services
        var serviceProvider = new ServiceCollection()
            .AddAWSService<IAmazonSQS>()
            .AddAWSService<IAmazonSimpleNotificationService>()
            .AddAWSService<IAmazonSecurityTokenService>()
            .AddAWSService<IAmazonIdentityManagementService>()
            .AddDefaultAWSOptions(defaultAwsOptions)
            .BuildServiceProvider();

        SqsClient = serviceProvider.GetService<IAmazonSQS>();
        StsClient = serviceProvider.GetService<IAmazonSecurityTokenService>();
        IamClient = serviceProvider.GetService<IAmazonIdentityManagementService>();

        var snsClient = serviceProvider.GetService<IAmazonSimpleNotificationService>()!;

        var successTopicArn = snsClient.FindTopicAsync(SuccessTopicName).Result.TopicArn;
        var failureTopicArn = snsClient.FindTopicAsync(SuccessTopicName).Result.TopicArn;

        dispatchCallbacks ??= DispatchCallbacksCollectionFactory.GetDefaultCallbacksCollectionWithSns(
            successTopicArn,
            failureTopicArn
        );

        Host = ServiceHelper
            .CreateServiceHost(
                configureServices: services =>
                    services
                        .AddDefaultAWSOptions(defaultAwsOptions)
                        .AddSingleton<ILogger>(NullLogger.Instance)
                        .AddAWSService<IAmazonSimpleNotificationService>()
                        .AddServiceModelServices()
                        .AddQueueTransport()
                        .AddSQSClient(queueName),
                configure: app =>
                {
                    var queueUrl = app.EnsureSqsQueue(queueName, createQueue);

                    app.UseServiceModel(services =>
                    {
                        services.AddService<LoggingService>();
                        services.AddServiceEndpoint<LoggingService, ILoggingService>(
                            new AwsSqsBinding { DispatchCallbacksCollection = dispatchCallbacks },
                            queueUrl
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
}
