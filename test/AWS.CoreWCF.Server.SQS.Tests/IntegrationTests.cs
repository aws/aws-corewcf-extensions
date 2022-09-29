using Amazon.Extensions.NETCore.Setup;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using AWS.CoreWCF.Server.SQS.Tests.TestService;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using CoreWCF.Channels;
using CoreWCF.Queue.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;
using AWS.CoreWCF.Server.SQS.CoreWCF.DispatchCallbacks;
using AWS.CoreWCF.Server.SQS.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;

namespace AWS.CoreWCF.Server.SQS.Tests;

[Collection("Environment collection")]
public class IntegrationTests : EnvironmentCollectionFixture
{
    private readonly IWebHost _host;

    public IntegrationTests(ITestOutputHelper output)
    {
        _host = ServiceHelper.CreateServiceHost<Startup>().Build();
        _host.Start();
    }

    [Fact]
    public async Task Server_Reads_And_Dispatches_Message_From_Sqs()
    {
        var testCaseName = nameof(Server_Reads_And_Dispatches_Message_From_Sqs);
        LoggingService.InitializeTestCase(testCaseName);

        await MessageHelper.SendMessageToQueueAsync(
            nameof(ILoggingService), 
            nameof(ILoggingService.LogMessage),
            testCaseName);

        Assert.True(LoggingService.LogResults[testCaseName].Wait(TimeSpan.FromSeconds(5)));
    }

    public class Startup
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
            services.AddDefaultAWSOptions(new AWSOptions
            {
                Credentials = GetCredentials()
            });
            services.AddAWSService<IAmazonSQS>();
            services.AddAWSService<IAmazonSimpleNotificationService>();
            services.AddQueueTransport();
        }

        public void Configure(IApplicationBuilder app)
        {
            var queueUrl = GetTestQueueUrl() ?? string.Empty;
            var concurrencyLevel = 1;
            var successTopicArn = SuccessTopicArn;
            var failureTopicArn = FailureTopicArn;
            app.UseServiceModel(services =>
            {
                services.AddService<LoggingService>();
                services.AddServiceEndpoint<LoggingService, ILoggingService>(
                    new AwsSqsBinding(
                        queueUrl, 
                        concurrencyLevel,
                        new DispatchCallbacksCollection(
                            successTopicArn,
                            failureTopicArn,
                            DispatchCallbacks.DefaultSuccessNotificationCallbackWithSns,
                            DispatchCallbacks.DefaultFailureNotificationCallbackWithSns)
                    ),
            "/BasicSqsService/ILoggingService.svc");
        });
        }
    }
}