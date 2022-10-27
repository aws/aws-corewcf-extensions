using System.IO.Pipelines;
using System.Text;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

internal class AwsSqsTransport : IQueueTransport
{
    private readonly IServiceProvider _services;
    private readonly Uri _baseAddress;
    private readonly string _queueName;
    private readonly Encoding _encoding;
    private readonly int _concurrencyLevel;
    private readonly IDispatchCallbacksCollection _dispatchCallbacksCollection;
    private readonly SQSMessageProvider _sqsMessageProvider;

    public int ConcurrencyLevel => _concurrencyLevel;

    public AwsSqsTransport(
        IServiceProvider services,
        IServiceDispatcher serviceDispatcher,
        string queueName,
        Encoding encoding,
        IDispatchCallbacksCollection dispatchCallbacksCollection,
        int concurrencyLevel = 1)
    {
        _services = services;
        _baseAddress = serviceDispatcher.BaseAddress;
        _queueName = queueName;
        _encoding = encoding;
        _concurrencyLevel = concurrencyLevel;
        _dispatchCallbacksCollection = dispatchCallbacksCollection;
        _sqsMessageProvider = _services.GetRequiredService<SQSMessageProvider>();
    }

    public async ValueTask<QueueMessageContext> ReceiveQueueMessageContextAsync(CancellationToken cancellationToken)
    {
        var sqsMessage = await _sqsMessageProvider.ReceiveMessageAsync(_queueName);

        if (sqsMessage is null)
        {
            return null;
        }

        var queueMessageContext = GetContext(sqsMessage);
        return queueMessageContext;
    }

    private QueueMessageContext GetContext(Message sqsMessage)
    {
        var reader = PipeReader.Create(sqsMessage.Body.ToStream(_encoding));
        var receiptHandle = sqsMessage.ReceiptHandle;
        var context = new AwsSqsMessageContext
        {
            QueueMessageReader = reader,
            LocalAddress = new EndpointAddress(_baseAddress),
            Properties = new Dictionary<string, object>(),
            DispatchResultHandler = MessageResultCallback,
            MessageReceiptHandle = receiptHandle
        };
        return context;
    }
    
    private async Task MessageResultCallback(QueueDispatchResult dispatchResult, QueueMessageContext queueMessageContext)
    {
        if (dispatchResult == QueueDispatchResult.Processed)
        {
            var notificationCallback = _dispatchCallbacksCollection.NotificationDelegateForSuccessfulDispatch;
            await notificationCallback.Invoke(_services, queueMessageContext);
            
            var receiptHandle = (queueMessageContext as AwsSqsMessageContext)?.MessageReceiptHandle;
            await _sqsMessageProvider.DeleteSqsMessageAsync(_queueName, receiptHandle);
        }

        if (dispatchResult == QueueDispatchResult.Failed)
        {
            var notificationCallback = _dispatchCallbacksCollection.NotificationDelegateForFailedDispatch;
            await notificationCallback.Invoke(_services, queueMessageContext);
        }
    }
}
