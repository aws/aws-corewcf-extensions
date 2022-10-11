using System.IO.Pipelines;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

internal class AwsSqsTransport : IQueueTransport
{
    private readonly IServiceProvider _services;
    private readonly Uri _baseAddress;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly Encoding _encoding;
    private readonly int _concurrencyLevel = 1;

    private readonly IDispatchCallbacksCollection _dispatchCallbacksCollection;

    public int ConcurrencyLevel => _concurrencyLevel;

    public AwsSqsTransport(
        IServiceProvider services,
        IServiceDispatcher serviceDispatcher,
        string queueUrl,
        Encoding encoding,
        IDispatchCallbacksCollection dispatchCallbacksCollection,
        int concurrencyLevel = 1)
    {
        _services = services;
        _baseAddress = serviceDispatcher.BaseAddress;
        _sqsClient = _services.GetRequiredService<IAmazonSQS>();
        _queueUrl = queueUrl;
        _encoding = encoding;
        _concurrencyLevel = concurrencyLevel;
        _dispatchCallbacksCollection = dispatchCallbacksCollection;
    }

    public async ValueTask<QueueMessageContext> ReceiveQueueMessageContextAsync(CancellationToken cancellationToken)
    {
        var sqsMessage = await GetSqsMessageAsync();

        if (sqsMessage is null)
        {
            return null;
        }

        var queueMessageContext = GetContext(sqsMessage);
        return queueMessageContext;
    }

    private async Task<Message?> GetSqsMessageAsync()
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = 1
        };
        try
        {
            var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(request);
            receiveMessageResponse.Validate();

            return receiveMessageResponse.Messages.FirstOrDefault();
        }
        catch (Exception e)
        {
            //_logger.LogError("Error occurred when trying to receive message from SQS: {}", e);
            return null;
        }
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
            await DeleteSqsMessageAsync(receiptHandle);
        }

        if (dispatchResult == QueueDispatchResult.Failed)
        {
            var notificationCallback = _dispatchCallbacksCollection.NotificationDelegateForFailedDispatch;
            await notificationCallback.Invoke(_services, queueMessageContext);
        }
    }

    private async Task DeleteSqsMessageAsync(string? receiptHandle)
    {
        var deleteMessageRequest = new DeleteMessageRequest
        {
            QueueUrl = _queueUrl,
            ReceiptHandle = receiptHandle
        };
        var deleteMessageResponse = await _sqsClient.DeleteMessageAsync(deleteMessageRequest);
        deleteMessageResponse.Validate();
    }
}
