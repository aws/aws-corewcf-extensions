using System.Diagnostics.CodeAnalysis;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF.Channels;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

/// <summary>
/// TODO
/// </summary>
public class AwsSqsBinding : Binding
{
    private readonly TextMessageEncodingBindingElement _encoding;
    private readonly AwsSqsTransportBindingElement _transport;

    /// <summary>
    /// Creates an SQS binding
    /// </summary>
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
