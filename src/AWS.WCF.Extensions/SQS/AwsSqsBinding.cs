using System.Diagnostics.CodeAnalysis;
using System.ServiceModel.Channels;
using Amazon.SQS;

namespace AWS.WCF.Extensions.SQS;

/// <summary>
/// Creates a new <see cref="Binding"/> a WCF Client
/// can use to send messages to a CoreWCF Service using
/// Amazon SQS as a transport.
/// </summary>
public class AwsSqsBinding : Binding
{
    [ExcludeFromCodeCoverage]
    public override string Scheme => SqsConstants.Scheme;

    /// <summary>
    /// Url of the queue
    /// </summary>
    public string QueueUrl { get; }

    /// <summary>
    /// Gets the encoding binding element
    /// </summary>
    public TextMessageEncodingBindingElement? Encoding { get; }

    /// <summary>
    /// Gets the SQS transport binding element
    /// </summary>
    public AwsSqsTransportBindingElement? Transport { get; }

    /// <inheritdoc cref="AwsSqsBinding"/>
    /// <param name="sqsClient">
    /// A fully constructed <see cref="IAmazonSQS"/> client.
    /// For more details on how to construct an sqs client, see
    /// https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/sqs-apis-intro.html
    /// </param>
    /// <param name="queueName">
    /// The name of the Amazon SQS Queue to use as a transport.
    /// </param>
    /// <param name="maxMessageSize"></param>
    /// <param name="maxBufferPoolSize"></param>
    public AwsSqsBinding(
        IAmazonSQS sqsClient,
        string queueName,
        long maxMessageSize = SqsDefaults.MaxSendMessageSize,
        long maxBufferPoolSize = SqsDefaults.MaxBufferPoolSize
    )
    {
        QueueUrl = sqsClient.GetQueueUrlAsync(queueName).Result.QueueUrl;
        Transport = new AwsSqsTransportBindingElement(sqsClient, QueueUrl, maxMessageSize, maxBufferPoolSize);
        Encoding = new TextMessageEncodingBindingElement();
    }

    public override BindingElementCollection CreateBindingElements()
    {
        var bindingElementCollection = new BindingElementCollection { Encoding, Transport };
        return bindingElementCollection.Clone();
    }
}
