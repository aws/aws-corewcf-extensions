using System.ServiceModel;
using System.ServiceModel.Channels;

namespace AWS.WCF.Extensions.SQS;

public class SqsChannelFactory : ChannelFactoryBase<IOutputChannel>
{
    private BufferManager _bufferManager;
    private MessageEncoderFactory _messageEncoderFactory;
    private AwsSqsTransportBindingElement _bindingElement;

    internal SqsChannelFactory(AwsSqsTransportBindingElement bindingElement, BindingContext context)
        : base(context.Binding)
    {
        _bindingElement = bindingElement;
        _bufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, int.MaxValue);

        IEnumerable<MessageEncodingBindingElement> messageEncoderBindingElements = context.BindingParameters
            .OfType<MessageEncodingBindingElement>()
            .ToList();

        if (messageEncoderBindingElements.Count() > 1)
        {
            throw new InvalidOperationException(
                "More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext"
            );
        }

        if (messageEncoderBindingElements.Count() == 1)
        {
            _messageEncoderFactory = messageEncoderBindingElements.First().CreateMessageEncoderFactory();
        }
        else
        {
            _messageEncoderFactory = SqsConstants.DefaultMessageEncoderFactory;
        }
    }

    public BufferManager BufferManager => _bufferManager;

    public MessageEncoderFactory MessageEncoderFactory => _messageEncoderFactory;

    public override T GetProperty<T>()
    {
        T messageEncoderProperty = this.MessageEncoderFactory.Encoder.GetProperty<T>();
        if (messageEncoderProperty != null)
        {
            return messageEncoderProperty;
        }

        if (typeof(T) == typeof(MessageVersion))
        {
            return (T)(object)this.MessageEncoderFactory.Encoder.MessageVersion;
        }

        return base.GetProperty<T>();
    }

    protected override void OnOpen(TimeSpan timeout) { }

    protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
    {
        return Task.CompletedTask;
    }

    protected override void OnEndOpen(IAsyncResult result) { }

    /// <summary>
    /// Create a new Udp Channel. Supports IOutputChannel.
    /// </summary>
    /// <typeparam name="TChannel">The type of Channel to create (e.g. IOutputChannel)</typeparam>
    /// <param name="queueUrl">The address of the remote endpoint</param>
    /// <returns></returns>
    protected override IOutputChannel OnCreateChannel(EndpointAddress queueUrl, Uri via)
    {
        return new SqsOutputChannel(this, _bindingElement.SqsClient, queueUrl, via, MessageEncoderFactory.Encoder);
    }

    protected override void OnClosed()
    {
        base.OnClosed();
        _bufferManager.Clear();
    }
}
