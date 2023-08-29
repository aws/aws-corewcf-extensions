using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using AWS.WCF.Extensions.SQS.Runtime;

namespace AWS.WCF.Extensions.SQS;

public class SqsChannelFactory : ChannelFactoryBase<IOutputChannel>
{
    private readonly AwsSqsTransportBindingElement _bindingElement;
    public BufferManager BufferManager { get; }
    public MessageEncoderFactory MessageEncoderFactory { get; }

    internal SqsChannelFactory(AwsSqsTransportBindingElement bindingElement, BindingContext context)
        : base(context.Binding)
    {
        _bindingElement = bindingElement;
        BufferManager = BufferManager.CreateBufferManager(bindingElement.MaxBufferPoolSize, int.MaxValue);

        IEnumerable<MessageEncodingBindingElement> messageEncoderBindingElements = context.BindingParameters
            .OfType<MessageEncodingBindingElement>()
            .ToList();

        if (messageEncoderBindingElements.Count() > 1)
        {
            throw new InvalidOperationException(
                "More than one MessageEncodingBindingElement was found in the BindingParameters of the BindingContext"
            );
        }

        MessageEncoderFactory = messageEncoderBindingElements.Any()
            ? messageEncoderBindingElements.First().CreateMessageEncoderFactory()
            : SqsConstants.DefaultMessageEncoderFactory;
    }

    public override T GetProperty<T>()
    {
        if (typeof(T) == typeof(MessageVersion))
            return (T)(object)MessageEncoderFactory.Encoder.MessageVersion;

        return MessageEncoderFactory.Encoder.GetProperty<T>() ?? base.GetProperty<T>();
    }

    /// <summary>
    /// Create a new Udp Channel. Supports IOutputChannel.
    /// </summary>
    /// <param name="queueUrl">The address of the remote endpoint</param>
    /// <param name="via"></param>
    protected override IOutputChannel OnCreateChannel(EndpointAddress queueUrl, Uri via)
    {
        return new SqsOutputChannel(this, _bindingElement.SqsClient, queueUrl, via, MessageEncoderFactory.Encoder);
    }

    /// <summary>
    /// Open the channel for use. We do not have any blocking work to perform so this is a no-op
    /// </summary>
    protected override void OnOpen(TimeSpan timeout) { }

    [ExcludeFromCodeCoverage]
    protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
    {
        return Task.CompletedTask.ToApm(callback, state);
    }

    [ExcludeFromCodeCoverage]
    protected override void OnEndOpen(IAsyncResult result)
    {
        result.ToApmEnd();
    }

    [ExcludeFromCodeCoverage]
    protected override void OnClosed()
    {
        base.OnClosed();
        BufferManager.Clear();
    }
}
