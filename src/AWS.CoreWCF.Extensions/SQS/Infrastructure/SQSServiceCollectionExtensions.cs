using Amazon.SQS;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class SQSServiceCollectionExtensions
{
    /// <inheritdoc cref="AddSQSClient(IServiceCollection,IEnumerable{string},Func{IServiceProvider,IAmazonSQS}?)"/>
    /// <param name="services">
    /// <see cref="IServiceCollection"/> container.
    /// </param>
    /// <param name="queueName">
    /// Names of the Amazon SQS Queues that this CoreWCF Server
    /// will listen to.
    /// </param>
    /// <param name="sqsClientBuilder">
    /// Optional function to build the <see cref="IAmazonSQS"/> client.  This is useful
    /// in cases where you need to provide specific credentials or otherwise customize
    /// the client object.
    /// </param>
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
    /// Registers an <see cref="IAmazonSQS"/> client for use by a CoreWCF server and lists the
    /// queues the client will listen to.
    /// <para />
    /// This method can be invoked multiple times to register multiple clients.  See the README.md
    /// for more information on how multiple clients impact performance.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection"/> container.
    /// </param>
    /// <param name="queueNames">
    /// The names of one or more Amazon SQS Queues that this CoreWCF Server
    /// will listen to.
    /// </param>
    /// <param name="sqsClientBuilder">
    /// Optional function to build the <see cref="IAmazonSQS"/> client.  This is useful
    /// in cases where you need to provide specific credentials or otherwise customize
    /// the client object.
    /// </param>
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
        services.AddTransient<ISQSMessageProvider, SQSMessageProvider>();

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
