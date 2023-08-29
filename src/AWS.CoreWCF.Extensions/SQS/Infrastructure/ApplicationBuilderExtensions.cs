using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="queueName"></param>
    /// <param name="createQueueRequest"></param>
    /// <returns></returns>
    public static string EnsureSqsQueue(
        this IApplicationBuilder builder,
        string queueName,
        CreateQueueRequest? createQueueRequest = null
    )
    {
        createQueueRequest ??= new CreateQueueRequest(queueName);

        return EnsureSqsQueue(builder, queueName, () => createQueueRequest);
    }

    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="queueName"></param>
    /// <param name="createQueueRequestBuilder"></param>
    /// <returns></returns>
    public static string EnsureSqsQueue(
        this IApplicationBuilder builder,
        string queueName,
        Func<CreateQueueRequest> createQueueRequestBuilder
    )
    {
        var sqsClient = builder.ApplicationServices
            .GetServices<NamedSQSClientCollection>()
            .SelectMany(x => x)
            .FirstOrDefault(x => string.Equals(x.QueueName, queueName, StringComparison.InvariantCultureIgnoreCase))
            ?.SQSClient;

        if (null == sqsClient)
        {
            throw new ArgumentException(
                $"Failed to find matching {nameof(IAmazonSQS)} for queue [{queueName}].  "
                    + $"Ensure that you have first registered a SQS Client for this queue via "
                    + $"{nameof(SQSServiceCollectionExtensions)}.{nameof(SQSServiceCollectionExtensions.AddSQSClient)}()",
                nameof(queueName)
            );
        }

        return sqsClient.EnsureSQSQueue(queueName, createQueueRequestBuilder).Result;
    }
}
