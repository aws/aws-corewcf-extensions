using Amazon.Runtime.Internal.Util;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using CoreWCF.Queue.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

public sealed class AwsSqsTransportBindingElement : QueueBaseTransportBindingElement
{
    public const int DefaultMaxMessageSize = 262144; // Max size for SQS message is 262144 (2^18)

    /// <summary>
    /// Creates a new instance of the AwsSqsTransportBindingElement class
    /// </summary>
    /// <param name="concurrencyLevel">Maximum number of workers polling the queue for messages</param>
    public AwsSqsTransportBindingElement(int concurrencyLevel = 1)
    {
        ConcurrencyLevel = concurrencyLevel;
        MaxReceivedMessageSize = DefaultMaxMessageSize;
    }

    private AwsSqsTransportBindingElement(AwsSqsTransportBindingElement other)
    {
        DispatchCallbacksCollection = other.DispatchCallbacksCollection;
        QueueName = other.QueueName;
        ConcurrencyLevel = other.ConcurrencyLevel;
        MaxReceivedMessageSize = other.MaxReceivedMessageSize;
    }

    public override QueueTransportPump BuildQueueTransportPump(BindingContext context)
    {
        var services = context.BindingParameters.Find<IServiceProvider>();
        var serviceDispatcher = context.BindingParameters.Find<IServiceDispatcher>();
        var messageEncoding = context.Binding.Elements.Find<TextMessageEncodingBindingElement>().WriteEncoding;

        var transport = new AwsSqsTransport(
            services,
            serviceDispatcher,
            QueueName,
            messageEncoding,
            DispatchCallbacksCollection,
            services.GetService<ILogger<AwsSqsTransport>>(),
            ConcurrencyLevel
        );

        return QueueTransportPump.CreateDefaultPump(transport);
    }

    public override int ConcurrencyLevel { get; }

    /// <summary>
    /// Gets the scheme used by the binding, https
    /// </summary>
    public override string Scheme => "http";

    /// <summary>
    /// Specifies the name of the queue
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// Contains the collection of callbacks available to be called after a message is dispatched
    /// </summary>
    public IDispatchCallbacksCollection DispatchCallbacksCollection { get; set; }

    public override BindingElement Clone() => new AwsSqsTransportBindingElement(this);
}
