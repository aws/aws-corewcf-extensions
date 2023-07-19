using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF.Channels;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

public class AwsSqsBinding : Binding
{
    public const int DefaultMaxMessageSize = 262144; // Max size for SQS message is 262144 (2^18)

    /// <summary>
    /// Creates an SQS binding
    /// </summary>
    /// <param name="queueName">Name of the queue</param>
    /// <param name="concurrencyLevel">Maximum number of workers polling the queue for messages</param>
    /// <param name="dispatchCallbacksCollection">Collection of callbacks to be called after message dispatch</param>
    /// <param name="maxMessageSize">The maximum message size in bytes for messages in the queue</param>
    public AwsSqsBinding(
        string queueName,
        int concurrencyLevel,
        IDispatchCallbacksCollection dispatchCallbacksCollection,
        int maxMessageSize = DefaultMaxMessageSize
    )
    {
        QueueName = queueName;
        ConcurrencyLevel = concurrencyLevel;
        MaxMessageSize = maxMessageSize;
        Name = nameof(AwsSqsBinding);
        Namespace = "https://schemas.aws.sqs.com/2007/sqs/";

        Transport = new AwsSqsTransportBindingElement(
            QueueName,
            ConcurrencyLevel,
            dispatchCallbacksCollection,
            MaxMessageSize
        );
        Encoding = new TextMessageEncodingBindingElement();
        MaxMessageSize = DefaultMaxMessageSize;
    }
    
    public override BindingElementCollection CreateBindingElements() => new() { Encoding, Transport };

    /// <summary>
    /// Gets the scheme used by the binding, https
    /// </summary>
    public override string Scheme => "http";

    /// <summary>
    /// Specifies the name of the queue
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// Specifies the maximum number of workers polling the queue for messages
    /// </summary>
    public int ConcurrencyLevel { get; set; }

    /// <summary>
    /// Specifies the maximum encoded message size
    /// </summary>
    public long MaxMessageSize { get; set; }

    /// <summary>
    /// Gets the encoding binding element
    /// </summary>
    public TextMessageEncodingBindingElement? Encoding { get; private set; }

    /// <summary>
    /// Gets the SQS transport binding element
    /// </summary>
    public AwsSqsTransportBindingElement? Transport { get; private set; }

    
}
