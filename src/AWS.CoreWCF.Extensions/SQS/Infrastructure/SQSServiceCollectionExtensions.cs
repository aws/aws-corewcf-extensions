using Amazon;
using Amazon.SQS;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Extensions.NETCore.Setup;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class SQSServiceCollectionExtensions
{
    public static IServiceCollection AddSQSClient(this IServiceCollection services, string queueName, Action<AWSOptions> awsOptionsAction, Action<IAmazonSQS, string> sqsClientAction = null)
    {
        var queueNames = new List<string> { queueName };
        return AddSQSClient(services, queueNames, awsOptionsAction, sqsClientAction);
    }

    public static IServiceCollection AddSQSClient(this IServiceCollection services, IEnumerable<string> queueNames, Action<AWSOptions> awsOptionsAction, Action<IAmazonSQS, string> sqsClientAction = null)
    {
        AddAmazonSQSClient(services, queueNames, awsOptionsAction, sqsClientAction);
        return services;
    }

    public static IServiceCollection AddSQSClient(this IServiceCollection services, string queueName, AWSCredentials credentials, Action<IAmazonSQS, string> sqsClientAction = null)
    {
        var queueNames = new List<string> { queueName };
        return AddSQSClient(services, queueNames, credentials, sqsClientAction);
    }

    public static IServiceCollection AddSQSClient(this IServiceCollection services, IEnumerable<string> queueNames, AWSCredentials credentials, Action<IAmazonSQS, string> sqsClientAction = null)
    {
        AddAmazonSQSClient(services, queueNames, credentials, sqsClientAction);
        return services;
    }

    private static void AddAmazonSQSClient(IServiceCollection services, IEnumerable<string> queueNames, Action<AWSOptions> awsOptionsAction, Action<IAmazonSQS, string> sqsClientAction = null)
    {
        var awsOptions = new AWSOptions();
        awsOptionsAction(awsOptions);
        var sqsConfig = new AmazonSQSConfig
        {
            RegionEndpoint = awsOptions.Region
        };
        var sqsClient = new AmazonSQSClient(CreateCredentials(awsOptions), sqsConfig);
        
        foreach (var queueName in queueNames)
        {
            if (sqsClientAction != null)
            {
                sqsClientAction(sqsClient, queueName);
            }

            services.AddSingleton(new NamedSQSClient(queueName, sqsClient));
        }
        services.AddSingleton<SQSMessageProvider>();
    }

    private static void AddAmazonSQSClient(IServiceCollection services, IEnumerable<string> queueNames, AWSCredentials credentials, Action<IAmazonSQS, string> sqsClientAction = null)
    {
        var sqsClient = new AmazonSQSClient(credentials);
        
        foreach (var queueName in queueNames)
        {
            if (sqsClientAction != null)
            {
                sqsClientAction(sqsClient, queueName);
            }

            services.AddSingleton(new NamedSQSClient(queueName, sqsClient));
        }
        services.AddSingleton<SQSMessageProvider>();
    }

    private static AWSCredentials CreateCredentials(AWSOptions options)
    {
        if (options?.Credentials != null)
        {
            return options.Credentials;
        }

        if (!string.IsNullOrEmpty(options?.Profile))
        {
            var chain = new CredentialProfileStoreChain(options.ProfilesLocation);
            if (chain.TryGetAWSCredentials(options.Profile, out var awsCredentials))
            {
                return awsCredentials;
            }
        }
        
        var credentials = FallbackCredentialsFactory.GetCredentials();
        if (credentials == null)
        {
            throw new AmazonClientException("Failed to find AWS Credentials for constructing AWS service client");
        }
        return credentials;
    }
}
