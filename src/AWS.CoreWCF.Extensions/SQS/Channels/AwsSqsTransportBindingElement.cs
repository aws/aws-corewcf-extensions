// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using CoreWCF.Queue.Common.Configuration;
using CoreWCF.Queue.CoreWCF.Queue;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

public sealed class AwsSqsTransportBindingElement : QueueBaseTransportBindingElement
{

    /// <summary>
    /// Gets the scheme used by the binding, https
    /// </summary>
    public override string Scheme => "http";

    /// <summary>
    /// Specifies the url of the queue
    /// </summary>
    public string QueueUrl { get; set; }

    /// <summary>
    /// Contains the collection of callbacks available to be called after a message is dispatched
    /// </summary>
    public IDispatchCallbacksCollection DispatchCallbacksCollection { get; set; }

    /// <summary>
    /// Creates a new instance of the AwsSqsTransportBindingElement class
    /// </summary>
    /// <param name="queueUrl">Url of the queue</param>
    /// <param name="concurrencyLevel">Maximum number of workers polling the queue for messages</param>
    /// <param name="dispatchCallbacksCollection">Collection of callbacks to be called after message dispatch</param>
    /// <param name="maxMessageSize">The maximum message size in bytes for messages in the queue</param>
    public AwsSqsTransportBindingElement(
        string queueUrl, 
        int concurrencyLevel,
        IDispatchCallbacksCollection dispatchCallbacksCollection,
        long maxMessageSize = AwsSqsBinding.DefaultMaxMessageSize)
    {
        QueueUrl = queueUrl;
        ConcurrencyLevel = concurrencyLevel;
        DispatchCallbacksCollection = dispatchCallbacksCollection;
        MaxReceivedMessageSize = maxMessageSize;
    }

    private AwsSqsTransportBindingElement(AwsSqsTransportBindingElement other)
    {
        DispatchCallbacksCollection = other.DispatchCallbacksCollection;
        QueueUrl = other.QueueUrl;
        ConcurrencyLevel = other.ConcurrencyLevel;
        MaxReceivedMessageSize = other.MaxReceivedMessageSize;
    }

    public override QueueTransportPump BuildQueueTransportPump(BindingContext context)
    {
        var transport = GetAwsSqsTransportAsync(context);
        return QueueTransportPump.CreateDefaultPump(transport);
    }

    public override BindingElement Clone()
    {
        return new AwsSqsTransportBindingElement(this);
    }

    private IQueueTransport GetAwsSqsTransportAsync(BindingContext context)
    {
        var services = context.BindingParameters.Find<IServiceProvider>();
        var serviceDispatcher = context.BindingParameters.Find<IServiceDispatcher>();
        var messageEncoding = context.Binding.Elements.Find<TextMessageEncodingBindingElement>().WriteEncoding;

        return new AwsSqsTransport(
            services,
            serviceDispatcher,
            QueueUrl,
            messageEncoding,
            DispatchCallbacksCollection,
            ConcurrencyLevel);
    }
}
