using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Amazon.SQS;
using AWS.Extensions.Common;

namespace AWS.WCF.Extensions.SQS;

public class SqsOutputChannel : ChannelBase, IOutputChannel
{
    private EndpointAddress _queueUrl;
    private Uri _via;
    private MessageEncoder _encoder;
    private SqsChannelFactory _parent;
    private IAmazonSQS _sqsClient;

    internal SqsOutputChannel(SqsChannelFactory factory, IAmazonSQS sqsClient, EndpointAddress queueUrl, Uri via, MessageEncoder encoder)
        : base(factory)
    {
        if (!string.Equals(via.Scheme, SqsConstants.Scheme, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                "The scheme {0} specified in address is not supported.", via.Scheme), nameof(via));
        }

        _parent = factory;
        _queueUrl = queueUrl;
        _via = via;
        _encoder = encoder;
        _sqsClient = sqsClient;
    }

    #region IOutputChannel_Properties
    EndpointAddress IOutputChannel.RemoteAddress => _queueUrl;

    Uri IOutputChannel.Via => _via;
    #endregion

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
    protected override void OnOpen(TimeSpan timeout)
    {
    }

    protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
    {
        return Task.CompletedTask;
    }

    protected override void OnEndOpen(IAsyncResult result)
    {
    }

    protected override void OnAbort()
    {
    }
    
    protected override void OnClose(TimeSpan timeout)
    {
    }

    protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
    {
        return Task.CompletedTask;
    }

    protected override void OnEndClose(IAsyncResult result)
    {
    }

    #region Send_Synchronous
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
            var response = _sqsClient.SendMessageAsync(_queueUrl.ToString(), serializedMessage).Result;
            response.Validate();
        }
        finally
        {
            // we need to make sure buffers are always returned to the BufferManager
            _parent.BufferManager.ReturnBuffer(messageBuffer.Array);
        }
    }

    public void Send(Message message, TimeSpan timeout)
    {
        Send(message);
    }
    #endregion
    

    public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
    {
        return Task.CompletedTask;
    }

    public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
    {
        // UDP does not block so we do not need timeouts.
        return BeginSend(message, callback, state);
    }

    public void EndSend(IAsyncResult result)
    {
    }
}
