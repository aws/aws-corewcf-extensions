using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF.Channels;

namespace AWS.CoreWCF.Extensions.SQS.Channels
{
    public class AwsSqsBinding : Binding
    {
        private readonly IDispatchCallbacksCollection _dispatchCallbacksCollection;
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
        /// Gets the encoding binding element
        /// </summary>
        public TextMessageEncodingBindingElement? Encoding { get; private set; }

        /// <summary>
        /// Gets the SQS transport binding element
        /// </summary>
        public AwsSqsTransportBindingElement? Transport { get; private set; }

        /// <summary>
        /// Creates an SQS binding
        /// </summary>
        /// <param name="queueUrl">Url of the queue</param>
        /// <param name="concurrencyLevel">Maximum number of workers polling the queue for messages</param>
        /// <param name="dispatchCallbacksCollection">Collection of callbacks to be called after message dispatch</param>
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
            var elements = new BindingElementCollection { Encoding, Transport };
            return elements;
        }

        private void Initialize()
        {
            Transport = new AwsSqsTransportBindingElement(QueueUrl, ConcurrencyLevel, _dispatchCallbacksCollection, MaxMessageSize);
            Encoding = new TextMessageEncodingBindingElement();
            MaxMessageSize = DefaultMaxMessageSize;
        }
    }
}
