using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Extensions.NETCore.Setup;
using AWS.CoreWCF.Extensions.Common;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class SQSServiceCollectionExtensions
{
    public static IServiceCollection AddSQSClient(this IServiceCollection services, string queueName, Action<AWSOptions> awsOptionsAction, Action<IAmazonSQS, AWSOptions, string> sqsClientAction = null)
    {
        var queueNames = new List<string> { queueName };
        return AddSQSClient(services, queueNames, awsOptionsAction, sqsClientAction);
    }

    public static IServiceCollection AddSQSClient(this IServiceCollection services, IEnumerable<string> queueNames, Action<AWSOptions> awsOptionsAction, Action<IAmazonSQS, AWSOptions, string> sqsClientAction = null)
    {
        AddAmazonSQSClient(services, queueNames, awsOptionsAction, sqsClientAction);
        return services;
    }

    private static void AddAmazonSQSClient(IServiceCollection services, IEnumerable<string> queueNames, Action<AWSOptions> awsOptionsAction, Action<IAmazonSQS, AWSOptions, string> sqsClientAction = null)
    {
        var awsOptions = new AWSOptions();
        awsOptionsAction(awsOptions);
        var sqsConfig = new AmazonSQSConfig
        {
            RegionEndpoint = awsOptions.Region
        };
        var sqsClient = new AmazonSQSClient(CredentialsHelper.GetCredentials(awsOptions), sqsConfig);
        
        foreach (var queueName in queueNames)
        {
            if (sqsClientAction != null)
            {
                sqsClientAction(sqsClient, awsOptions, queueName);
            }

            services.AddSingleton(new NamedSQSClient(queueName, sqsClient));
        }
        services.AddSingleton<SQSMessageProvider>();
    }
}
