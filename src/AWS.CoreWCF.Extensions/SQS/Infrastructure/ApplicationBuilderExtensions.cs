using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class ApplicationBuilderExtensions
{
    /// <inheritdoc cref="EnsureSqsQueue(IApplicationBuilder,string,Func{CreateQueueRequest})"/>
    /// <param name="builder"></param>
    /// <param name="queueName">
    /// Name of the queue to create if it does not already exist.
    /// </param>
    /// <param name="createQueueRequest"></param>
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
    /// Helper function that checks to see if <paramref name="queueName"/> exists in Amazon SQS,
    /// if not, uses <paramref name="createQueueRequestBuilder"/> to construct.
    /// <para />
    /// This method returns the Queue Url for <paramref name="queueName"/>, which is required to invoke
    /// <see cref="IServiceBuilder.AddServiceEndpoint{TService,TContract}(Binding,string)"/>
    /// <example>
    /// <![CDATA[
    /// var app = builder.Build();
    ///
    /// var queueUrl = app.EnsureSqsQueue(_queueName);
    ///
    /// app.UseServiceModel(services =>
    /// {
    ///     services.AddService<ExampleService>();
    ///     services.AddServiceEndpoint<ExampleService, IExampleService>(
    ///         new AwsSqsBinding(),
    ///         queueUrl
    ///     );
    /// });
    /// ]]>
    /// </example>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="queueName">
    /// Name of the queue to create if it does not already exist.
    /// </param>
    /// <param name="createQueueRequestBuilder">
    /// Function for building a <see cref="CreateQueueRequest"/> object that will be used to construct
    /// <paramref name="queueName"/> in the event it does not yet exist.  <paramref name="createQueueRequestBuilder"/>
    /// is only invoked if <paramref name="queueName"/> does not exist.
    /// </param>
    /// <returns>
    /// The url for <paramref name="queueName"/>.
    /// </returns>
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
