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

namespace AWS.CoreWCF.Server.SQS.Tests;

[Collection("Environment collection")]
public class IntegrationTests : EnvironmentCollectionFixture
{
    private readonly ITestOutputHelper _output;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SomeTest()
    {
        using var host = ServiceHelper.CreateServiceHost<Startup>().Build();
        await host.StartAsync();
        //await MessageHelper.SendMessageToQueueAsync(nameof(ILoggingService), nameof(ILoggingService.LogMessage));

        //var loggingService = host.Services.GetService<LoggingService>();
        //Assert.True(loggingService.ManualResetEvent.Wait(TimeSpan.FromSeconds(5)));
        await Task.Delay(5000);
        Console.WriteLine("Waiting for newline character...");
    }

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddScoped<LoggingService>();
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
                    //queueUrl);
            "/BasicSqsService/ILoggingService.svc");
        });
        }
    }
}