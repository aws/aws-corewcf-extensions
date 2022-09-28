// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AWS.CoreWCF.Server.SQS.CoreWCF.DispatchCallbacks;

namespace CoreWCF.Channels
{
    public class AwsSqsBinding : Binding
    {
        private IDispatchCallbacksCollection _dispatchCallbacksCollection;
        private TextMessageEncodingBindingElement? _encoding;
        public const int DefaultMaxMessageSize = 262144;  // Max size for SQS message is 262144 (2^18)

        /// <summary>
        /// Gets the scheme used by the binding, https
        /// </summary>
        public override string Scheme => "http";

        /// <summary>
        /// Specifies the url of the queue
        /// </summary>
        public string QueueUrl { get; set; }

        /// <summary>
        /// Specifies the maximum number of workers polling the queue for messages
        /// </summary>
        public int ConcurrencyLevel { get; set; }

        /// <summary>
        /// Specifies the maximum encoded message size
        /// </summary>
        public long MaxMessageSize { get; set; }

        /// <summary>
        /// Gets the SQS transport binding element
        /// </summary>
        public AwsSqsTransportBindingElement? Transport { get; private set; }

        /// <summary>
        /// Creates an SQS binding
        /// </summary>
        /// <param name="queueUrl">Url of the queue</param>
        /// <param name="concurrencyLevel">Maximum number of workers polling the queue for messages</param>
        /// <param name="maxMessageSize">The maximum message size in bytes for messages in the queue</param>
        public AwsSqsBinding(string queueUrl, int concurrencyLevel, IDispatchCallbacksCollection dispatchCallbacksCollection, int maxMessageSize = DefaultMaxMessageSize)
        {
            QueueUrl = queueUrl;
            ConcurrencyLevel = concurrencyLevel;
            _dispatchCallbacksCollection = dispatchCallbacksCollection;
            MaxMessageSize = maxMessageSize;
            Name = nameof(AwsSqsBinding);
            Namespace = "https://schemas.aws.sqs.com/2007/sqs/";

            Initialize();
        }

        public override BindingElementCollection CreateBindingElements()
        {
            if (MaxMessageSize != DefaultMaxMessageSize)
            {
                Transport.MaxReceivedMessageSize = MaxMessageSize;
            }
            BindingElementCollection elements = new BindingElementCollection { _encoding, Transport, };

            return elements;
        }

        private void Initialize()
        {
            Transport = new AwsSqsTransportBindingElement(QueueUrl, ConcurrencyLevel, _dispatchCallbacksCollection, MaxMessageSize);
            _encoding = new TextMessageEncodingBindingElement();
            MaxMessageSize = DefaultMaxMessageSize;
        }
    }
}
