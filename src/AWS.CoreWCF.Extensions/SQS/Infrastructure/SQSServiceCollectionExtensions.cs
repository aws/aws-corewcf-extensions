using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class SQSServiceCollectionExtensions
{
    public static IServiceCollection AddSQSClient(
        this IServiceCollection services,
        string queueName,
        Func<IServiceProvider, IAmazonSQS>? sqsClientBuilder = null
    )
    {
        var queueNames = new List<string> { queueName };
        return AddSQSClient(services, queueNames, sqsClientBuilder);
    }

    public static IServiceCollection AddSQSClient(
        this IServiceCollection services,
        IEnumerable<string> queueNames,
        Func<IServiceProvider, IAmazonSQS>? sqsClientBuilder = null
    )
    {
        if (null == sqsClientBuilder)
        {
            services.AddAWSService<IAmazonSQS>();
            sqsClientBuilder = sp => sp.GetService<IAmazonSQS>();
        }

        AddAmazonSQSClient(services, queueNames, sqsClientBuilder);
        return services;
    }

    private static void AddAmazonSQSClient(
        IServiceCollection services,
        IEnumerable<string> queueNames,
        Func<IServiceProvider, IAmazonSQS> sqsClientBuilder
    )
    {
        services.AddSingleton<SQSMessageProvider>();

        services.AddSingleton<NamedSQSClientCollection>(serviceProvider =>
        {
            var sqsClient = sqsClientBuilder(serviceProvider);

            (sqsClient as AmazonSQSClient)?.SetCustomUserAgentSuffix();

            var namedSqsClients = new NamedSQSClientCollection(
                queueNames.Select(queueName => new NamedSQSClient { SQSClient = sqsClient, QueueName = queueName })
            );

            return namedSqsClients;
        });
    }

    public static string EnsureSqsQueue(
        this IApplicationBuilder builder,
        string queueName,
        CreateQueueRequest? createQueueRequest = null
    )
    {
        // TODO Harden
        var sqsClient = builder.ApplicationServices
            .GetServices<NamedSQSClientCollection>()
            .SelectMany(x => x)
            .FirstOrDefault(x => string.Equals(x.QueueName, queueName, StringComparison.InvariantCultureIgnoreCase))
            ?.SQSClient;

        createQueueRequest ??= new CreateQueueRequest(queueName);

        // TODO Harden
        sqsClient.EnsureSQSQueue(createQueueRequest);

        // TODO Harden
        var queueUrl = sqsClient.GetQueueUrlAsync(queueName).Result;

        return queueUrl.QueueUrl;
    }
}
