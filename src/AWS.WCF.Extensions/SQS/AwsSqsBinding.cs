using System.ServiceModel.Channels;
using Amazon.SQS;

namespace AWS.WCF.Extensions.SQS;

public class AwsSqsBinding : Binding
{
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
