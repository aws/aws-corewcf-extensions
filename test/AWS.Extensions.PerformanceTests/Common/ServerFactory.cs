using System.Diagnostics.CodeAnalysis;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using AWS.Extensions.IntegrationTests.SQS.TestService;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AWS.Extensions.PerformanceTests.Common;

[ExcludeFromCodeCoverage]
public static class ServerFactory
{
    public static IWebHost StartServer<TService, TContract>(string queueName, string queueUrl, AwsSqsBinding binding)
        where TService : class
    {
        return ServiceHelper
            .CreateServiceHost(
                configureServices: services =>
                    services
                        .AddServiceModelServices()
                        .AddSingleton<ILogger>(NullLogger.Instance)
                        .AddQueueTransport()
                        .AddSQSClient(queueName),
                configure: app =>
                {
                    app.UseServiceModel(svc =>
                    {
                        svc.AddService<TService>();
                        svc.AddServiceEndpoint<TService, TContract>(binding, queueUrl);
                    });
                }
            )
            .Build();
    }
}
