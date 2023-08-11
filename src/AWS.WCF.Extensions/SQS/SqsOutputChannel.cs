using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.WCF.Extensions.SQS.Runtime;
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

        (_sqsClient as AmazonSQSClient)?.SetCustomUserAgentSuffix();
    }

    EndpointAddress IOutputChannel.RemoteAddress => _queueUrl;

    Uri IOutputChannel.Via => _via;

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

    public override T GetProperty<T>()
    {
        if (typeof(T) == typeof(IOutputChannel))
            return (T)(object)this;

        return _encoder.GetProperty<T>() ?? base.GetProperty<T>();
    }

    /// <summary>
    /// Address the Message and serialize it into a byte array.
    /// </summary>
    internal ArraySegment<byte> EncodeMessage(Message message)
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

    #region Events
    /// <summary>
    /// Open the channel for use. We do not have any blocking work to perform so this is a no-op
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected override void OnOpen(TimeSpan timeout) { }

    /// <summary>
    /// no-op
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected override void OnAbort() { }

    /// <summary>
    /// no-op
    /// </summary>
    [ExcludeFromCodeCoverage]
    protected override void OnClose(TimeSpan timeout) { }
    #endregion

    #region OnBegin/OnEnd methods

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
    protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
    {
        return Task.CompletedTask.ToApm(callback, state);
    }

    [ExcludeFromCodeCoverage]
    protected override void OnEndClose(IAsyncResult result)
    {
        result.ToApmEnd();
    }

    [ExcludeFromCodeCoverage]
    public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
    {
        var task = Task.Run(() => Send(message));

        return task.ToApm(callback, state);
    }

    [ExcludeFromCodeCoverage]
    public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
    {
        return BeginSend(message, callback, state);
    }

    [ExcludeFromCodeCoverage]
    public void EndSend(IAsyncResult result)
    {
        result.ToApmEnd();
    }

    #endregion
}
