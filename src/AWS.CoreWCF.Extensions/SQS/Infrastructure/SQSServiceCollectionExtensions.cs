using Amazon.SQS;
using AWS.CoreWCF.Extensions.SQS.Channels;
using CoreWCF.Configuration;
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

            var namedSqsClients = new NamedSQSClientCollection(
                queueNames.Select(queueName => new NamedSQSClient(queueName, sqsClient))
            );

            return namedSqsClients;
        });
    }

    public static IServiceBuilder AddSQSServiceEndpoint<TService, TContract>(
        this IServiceBuilder serviceBuilder,
        IApplicationBuilder app,
        AwsSqsBinding awsSqsBinding,
        Func<IAmazonSQS, string, Task>? queueInitializer = null
    )
    {
        return AddSQSServiceEndpoint<TService, TContract>(
            serviceBuilder,
            app.ApplicationServices,
            awsSqsBinding,
            queueInitializer
        );
    }

    public static IServiceBuilder AddSQSServiceEndpoint<TService, TContract>(
        this IServiceBuilder serviceBuilder,
        IServiceProvider serviceProvider,
        AwsSqsBinding awsSqsBinding,
        Func<IAmazonSQS, string, Task>? queueInitializer = null
    )
    {
        if (string.IsNullOrEmpty(awsSqsBinding.QueueName))
            throw new ArgumentException("QueueName is required", nameof(awsSqsBinding));

        var matchingNamedSQSClient = serviceProvider
            .GetServices<NamedSQSClientCollection>()
            .SelectMany(x => x)
            .FirstOrDefault(x => x.QueueName == awsSqsBinding.QueueName);

        if (null == matchingNamedSQSClient)
            throw new ArgumentException(
                $"Invalid Binding: [{awsSqsBinding.QueueName}] must first be registered inside ${nameof(serviceProvider)} "
                    + $"via {nameof(SQSServiceCollectionExtensions)}.{nameof(AddSQSClient)}"
            );

        matchingNamedSQSClient.Initialize(queueInitializer).Wait();

        serviceBuilder.AddServiceEndpoint<TService, TContract>(awsSqsBinding, matchingNamedSQSClient.QueueUrl);

        return serviceBuilder;
    }
}
