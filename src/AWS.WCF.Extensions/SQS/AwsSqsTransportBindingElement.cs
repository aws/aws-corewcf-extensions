using System.ServiceModel.Channels;
using Amazon.SQS;

namespace AWS.WCF.Extensions.SQS;

public class AwsSqsTransportBindingElement : TransportBindingElement
{
    public override string Scheme => SqsConstants.Scheme;
    
    public IAmazonSQS SqsClient { get; set; }

    public string QueueName { get; set; }
    
    public override long MaxReceivedMessageSize { get; set; }

    /// <summary>
    /// Creates a new instance of the AwsSqsTransportBindingElement class
    /// </summary>
    /// <param name="sqsClient">Client used for accessing the queue</param>
    /// <param name="queueUrl">Url of the queue</param>
    /// <param name="maxMessageSize">The maximum message size in bytes for messages in the queue</param>
    /// <param name="maxBufferPoolSize">The maximum buffer pool size</param>
    public AwsSqsTransportBindingElement(
        IAmazonSQS sqsClient,
        string queueName,
        long maxMessageSize = SqsDefaults.MaxSendMessageSize,
        long maxBufferPoolSize = SqsDefaults.MaxBufferPoolSize)
    {
        SqsClient = sqsClient;
        QueueName = queueName;
        MaxReceivedMessageSize = maxMessageSize;
        MaxBufferPoolSize = maxBufferPoolSize;
    }

    protected AwsSqsTransportBindingElement(AwsSqsTransportBindingElement other)
    {
        SqsClient = other.SqsClient;
        QueueName = other.QueueName;
        MaxReceivedMessageSize = other.MaxReceivedMessageSize;
        MaxBufferPoolSize = other.MaxBufferPoolSize;
    }

    public override BindingElement Clone()
    {
        return new AwsSqsTransportBindingElement(this);
    }

    public override T GetProperty<T>(BindingContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return context.GetInnerProperty<T>();
    }

    public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return (IChannelFactory<TChannel>)(object)new SqsChannelFactory(this, context);
    }

    /// <summary>
    /// Used by higher layers to determine what types of channel factories this
    /// binding element supports. Which in this case is just IOutputChannel.
    /// </summary>
    public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
    {
        return (typeof(TChannel) == typeof(IOutputChannel));
    }
}
