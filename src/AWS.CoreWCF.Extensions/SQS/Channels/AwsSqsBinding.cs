using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF.Channels;
using CoreWCF.Configuration;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

/// <summary>
/// Constructs a new <see cref="Binding"/> that uses Amazon SQS as a transport
/// and can be registered in
/// <see cref="IServiceBuilder.AddServiceEndpoint{TService,TContract}(Binding,string)"/>
/// </summary>
public class AwsSqsBinding : Binding
{
    private readonly TextMessageEncodingBindingElement _encoding;
    private readonly AwsSqsTransportBindingElement _transport;

    /// <inheritdoc cref="AwsSqsBinding"/>
    /// <param name="concurrencyLevel">Maximum number of workers polling the queue for messages</param>
    public AwsSqsBinding(int concurrencyLevel = 1)
    {
        Name = nameof(AwsSqsBinding);
        Namespace = "https://schemas.aws.sqs.com/2007/sqs/";

        _encoding = new TextMessageEncodingBindingElement();

        _transport = new AwsSqsTransportBindingElement(concurrencyLevel);
    }

    public override BindingElementCollection CreateBindingElements() => new() { _encoding, _transport };

    /// <inheritdoc cref="AwsSqsTransportBindingElement.MaxReceivedMessageSize"/>
    public long MaxMessageSize
    {
        get => _transport.MaxReceivedMessageSize;
        set => _transport.MaxReceivedMessageSize = value;
    }

    /// <inheritdoc cref="AwsSqsTransportBindingElement.DispatchCallbacksCollection"/>
    public IDispatchCallbacksCollection DispatchCallbacksCollection
    {
        get => _transport.DispatchCallbacksCollection;
        set => _transport.DispatchCallbacksCollection = value;
    }

    /// <inheritdoc cref="AwsSqsTransportBindingElement.Scheme"/>
    public override string Scheme => _transport.Scheme;
}
