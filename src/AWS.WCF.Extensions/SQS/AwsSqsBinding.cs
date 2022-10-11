using System.ServiceModel.Channels;
using Amazon.SQS;

namespace AWS.WCF.Extensions.SQS;

public class AwsSqsBinding : Binding
{
    public override string Scheme => SqsConstants.Scheme;

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
        string queueUrl, 
        long maxMessageSize = SqsDefaults.MaxReceivedMessageSize, 
        long maxBufferPoolSize = SqsDefaults.MaxReceivedMessageSize)
    {
        Transport = new AwsSqsTransportBindingElement(sqsClient, queueUrl, maxMessageSize, maxBufferPoolSize);
        Encoding = new TextMessageEncodingBindingElement();
    }

    public override BindingElementCollection CreateBindingElements()
    {
        var bindingElementCollection = new BindingElementCollection { Encoding, Transport };
        return bindingElementCollection.Clone();
    }
}