// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.SQS;
using AWS.CoreWCF.Server.SQS.CoreWCF.DispatchCallbacks;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using CoreWCF.Queue.Common.Configuration;
using CoreWCF.Queue.CoreWCF.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace CoreWCF.Channels
{
    public sealed class AwsSqsTransportBindingElement : QueueBaseTransportBindingElement
    {
        private readonly IDispatchCallbacksCollection _dispatchCallbacksCollection;

        /// <summary>
        /// Gets the scheme used by the binding, https
        /// </summary>
        public override string Scheme => "http";

        /// <summary>
        /// Specifies the url of the queue
        /// </summary>
        public string QueueUrl { get; set; }

        /// <summary>
        /// Creates a new instance of the AwsSqsTransportBindingElement class
        /// </summary>
        /// <param name="queueUrl">Url of the queue</param>
        /// <param name="concurrencyLevel">Maximum number of workers polling the queue for messages</param>
        /// <param name="maxMessageSize">The maximum message size in bytes for messages in the queue</param>
        public AwsSqsTransportBindingElement(
            string queueUrl, 
            int concurrencyLevel,
            IDispatchCallbacksCollection dispatchCallbacksCollection,
            long maxMessageSize = AwsSqsBinding.DefaultMaxMessageSize)
        {
            QueueUrl = queueUrl;
            ConcurrencyLevel = concurrencyLevel;
            _dispatchCallbacksCollection = dispatchCallbacksCollection;
            MaxReceivedMessageSize = maxMessageSize;
        }

        private AwsSqsTransportBindingElement(AwsSqsTransportBindingElement other)
        {
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
                _dispatchCallbacksCollection,
                ConcurrencyLevel);
        }
    }
}
