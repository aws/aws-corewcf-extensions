using Amazon.SQS;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class SQSServiceCollectionExtensions
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="services"></param>
    /// <param name="queueName"></param>
    /// <param name="sqsClientBuilder"></param>
    /// <returns></returns>
    public static IServiceCollection AddSQSClient(
        this IServiceCollection services,
        string queueName,
        Func<IServiceProvider, IAmazonSQS>? sqsClientBuilder = null
    )
    {
        var queueNames = new List<string> { queueName };
        return AddSQSClient(services, queueNames, sqsClientBuilder);
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="services"></param>
    /// <param name="queueNames"></param>
    /// <param name="sqsClientBuilder"></param>
    /// <returns></returns>
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
}
