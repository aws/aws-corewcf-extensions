using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using Microsoft.AspNetCore.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

[Collection("ClientAndServer collection")]
public class SqsIntegrationTests
{
    private readonly IWebHost _host;
    private readonly ITestOutputHelper _output;
    private static ClientAndServerFixture _clientAndServerFixture;

    public SqsIntegrationTests(ITestOutputHelper output, ClientAndServerFixture clientAndServerFixture)
    {
        _output = output;
        _clientAndServerFixture = clientAndServerFixture;

        //_host = ServiceHelper.CreateServiceHost<Startup>().Build();
        //_host.Start();
    }

    [Fact]
    public async Task Server_Reads_And_Dispatches_Message_From_Sqs()
    {
        var testCaseName = nameof(Server_Reads_And_Dispatches_Message_From_Sqs);
        LoggingService.InitializeTestCase(testCaseName);

        //await MessageHelper.SendMessageToQueueAsync(
        //    nameof(ILoggingService),
        //    nameof(ILoggingService.LogMessage),
        //    testCaseName,
        //    _clientAndServerFixture.QueueUrl,
        //    _clientAndServerFixture.GetCredentials());

        Assert.True(LoggingService.LogResults[testCaseName].Wait(TimeSpan.FromSeconds(5)));
    }

    //    public class Startup
    //    {
    //        public void ConfigureServices(IServiceCollection services)
    //        {
    //            services.AddServiceModelServices();
    //            services.AddSingleton<ILogger>(NullLogger.Instance);
    //#if DEBUG
    //            services.AddLogging(builder =>
    //            {
    //                builder.AddConsole();
    //            });
    //#endif
    //            services.AddDefaultAWSOptions(new AWSOptions
    //            {
    //                Credentials = _clientAndServerFixture.GetCredentials()
    //            });
    //            services.AddAWSService<IAmazonSQS>();
    //            services.AddAWSService<IAmazonSimpleNotificationService>();
    //            services.AddQueueTransport();
    //        }

    //        public void Configure(IApplicationBuilder app)
    //        {
    //            var queueUrl = _clientAndServerFixture.QueueUrl;
    //            var concurrencyLevel = 1;
    //            var successTopicArn = _clientAndServerFixture.SuccessTopicArn;
    //            var failureTopicArn = _clientAndServerFixture.FailureTopicArn;
    //            app.UseServiceModel(services =>
    //            {
    //                services.AddService<LoggingService>();
    //                services.AddServiceEndpoint<LoggingService, ILoggingService>(
    //                    new AwsSqsBinding(
    //                        queueUrl,
    //                        concurrencyLevel,
    //                        DispatchCallbacksCollectionFactory.GetDefaultCallbacksCollectionWithSns(successTopicArn, failureTopicArn)
    //                    ),
    //            "/BasicSqsService/ILoggingService.svc");
    //            });
    //        }
    //    }
}