using System.ServiceModel;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
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

namespace AWS.Extensions.IntegrationTests.SQS;

[CollectionDefinition("ClientAndServer collection")]
public class ClientAndServerCollectionFixture : ICollectionFixture<ClientAndServerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

public class ClientAndServerFixture : IDisposable
{
    private const string AwsKey = "AWS";
    private const string ProfileEnvVariable = "PROFILE";

    //private const string AccessKeyEnvVariable = "AWS_ACCESS_KEY_ID";
    //private const string SecretKeyEnvVariable = "AWS_SECRET_ACCESS_KEY";
    //private const string TestQueueNameEnvVariable = "TEST_QUEUE_NAME";
    private const string SuccessTopicArnEnvVariable = "SUCCESS_TOPIC_ARN";
    private const string FailureTopicArnEnvVariable = "FAILURE_TOPIC_ARN";

    private ChannelFactory<ILoggingService> _factory;

    public static string Profile { get; set; } = string.Empty;

    //public static string AccessKey { get; set; } = string.Empty;
    //public static string SecretKey { get; set; } = string.Empty;
    //public static string QueueName { get; set; } = string.Empty;
    public static string SuccessTopicArn { get; set; } = string.Empty;
    public static string FailureTopicArn { get; set; } = string.Empty;
    public const string QueueWithDefaultSettings = "CoreWCFExtensionsDefaultSettingsQueue";
    public const string FifoQueueName = "CoreWCFExtensionsTest.fifo";

    public IWebHost Host { get; private set; }
    public ILoggingService Channel { get; private set; }
    public IAmazonSQS SqsClient { get; }

    public ClientAndServerFixture()
    {
        ReadTestEnvironmentSettingsFromFile(Path.Combine("SQS", "appsettings.test.json"));
        SqsClient = new AmazonSQSClient(CredentialsHelper.GetCredentials(new AWSOptions { Profile = Profile }));

        CreateAndStartHost();
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

    private void EnsureQueueIsEmpty()
    {
        //var response = SqsClient.PurgeQueueAsync(QueueUrl).Result;
        //response.Validate();
    }

    private void CreateAndStartHost()
    {
        Host = ServiceHelper.CreateServiceHost<Startup>().Build();
        Host.Start();
    }

    private void CreateAndOpenClientChannel()
    {
        var sqsBinding = new WCF.Extensions.SQS.AwsSqsBinding(SqsClient, QueueWithDefaultSettings);
        var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
        _factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
        Channel = _factory.CreateChannel();
        ((System.ServiceModel.Channels.IChannel)Channel).Open();
    }

    private void ReadTestEnvironmentSettingsFromFile(string settingsFilePath)
    {
        var json = File.ReadAllText(settingsFilePath);
        var appSettingsDictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

        var settingsDict = appSettingsDictionary[AwsKey];
        Profile = settingsDict[ProfileEnvVariable];
        SuccessTopicArn = settingsDict[SuccessTopicArnEnvVariable];
        FailureTopicArn = settingsDict[FailureTopicArnEnvVariable];
    }

    private class Startup
    {
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

            services.AddSQSClient(
                QueueWithDefaultSettings,
                (awsOptions) =>
                {
                    awsOptions.Profile = Profile;
                },
                (sqsClient, awsOptions, queueName) =>
                {
                    sqsClient.EnsureSQSQueue(awsOptions, new CreateQueueRequest(queueName).SetDefaultValues());
                }
            );

            services.AddSQSClient(
                FifoQueueName,
                (awsOptions) =>
                {
                    awsOptions.Profile = Profile;
                },
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

            var successTopicArn = SuccessTopicArn;
            var failureTopicArn = FailureTopicArn;

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
                    "/BasicSqsService/ILoggingService.svc"
                );
            });
        }
    }
}
