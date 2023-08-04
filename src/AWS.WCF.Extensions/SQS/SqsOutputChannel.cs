using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Message = System.ServiceModel.Channels.Message;

namespace AWS.WCF.Extensions.SQS;

public class SqsOutputChannel : ChannelBase, IOutputChannel
{
    private readonly EndpointAddress _queueUrl;
    private readonly Uri _via;
    private readonly MessageEncoder _encoder;
    private readonly SqsChannelFactory _parent;
    private readonly IAmazonSQS _sqsClient;

    internal SqsOutputChannel(
        SqsChannelFactory factory,
        IAmazonSQS sqsClient,
        EndpointAddress queueUrl,
        Uri via,
        MessageEncoder encoder
    )
        : base(factory)
    {
        if (!string.Equals(via.Scheme, SqsConstants.Scheme, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "The scheme {0} specified in address is not supported.",
                    via.Scheme
                ),
                nameof(via)
            );
        }

        _parent = factory;
        _queueUrl = queueUrl;
        _via = via;
        _encoder = encoder;
        _sqsClient = sqsClient;
    }

    EndpointAddress IOutputChannel.RemoteAddress => _queueUrl;

    Uri IOutputChannel.Via => _via;

    public override T GetProperty<T>()
    {
        if (typeof(T) == typeof(IOutputChannel))
        {
            return (T)(object)this;
        }

        T messageEncoderProperty = this._encoder.GetProperty<T>();
        if (messageEncoderProperty != null)
        {
            return messageEncoderProperty;
        }

        return base.GetProperty<T>();
    }

    /// <summary>
    /// Open the channel for use. We do not have any blocking work to perform so this is a no-op
    /// </summary>
    protected override void OnOpen(TimeSpan timeout) { }

    protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
    {
        return Task.CompletedTask;
    }

    protected override void OnEndOpen(IAsyncResult result) { }

    protected override void OnAbort() { }

    protected override void OnClose(TimeSpan timeout) { }

    protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
    {
        return Task.CompletedTask;
    }

    protected override void OnEndClose(IAsyncResult result) { }

    /// <summary>
    /// Address the Message and serialize it into a byte array.
    /// </summary>
    ArraySegment<byte> EncodeMessage(Message message)
    {
        try
        {
            _queueUrl.ApplyTo(message);
            return _encoder.WriteMessage(message, int.MaxValue, _parent.BufferManager);
        }
        finally
        {
            // We have consumed the message by serializing it, so clean up
            message.Close();
        }
    }

    public void Send(Message message)
    {
        var messageBuffer = EncodeMessage(message);

        try
        {
            var serializedMessage = Encoding.UTF8.GetString(messageBuffer.ToArray());
            var sendMessageRequest = new SendMessageRequest
            {
                MessageBody = serializedMessage,
                QueueUrl = _queueUrl.ToString()
            };
            if (_queueUrl.ToString().EndsWith(".fifo", StringComparison.InvariantCultureIgnoreCase))
            {
                sendMessageRequest.MessageGroupId = _queueUrl.ToString();
            }
            var response = _sqsClient.SendMessageAsync(sendMessageRequest).Result;
            response.Validate();
        }
        finally
        {
            // Make sure buffers are always returned to the BufferManager
            _parent.BufferManager.ReturnBuffer(messageBuffer.Array);
        }
    }

    public void Send(Message message, TimeSpan timeout)
    {
        Send(message);
    }

    public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
    {
        return Task.CompletedTask;
    }

    public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
    {
        return BeginSend(message, callback, state);
    }

    public void EndSend(IAsyncResult result) { }
}
