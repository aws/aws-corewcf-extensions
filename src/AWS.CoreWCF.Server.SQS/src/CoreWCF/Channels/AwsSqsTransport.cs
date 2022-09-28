// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Text;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Server.Common;
using AWS.CoreWCF.Server.SQS.CoreWCF.DispatchCallbacks;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CoreWCF.Channels
{
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
            _sqsClient = _services.GetRequiredService<AmazonSQSClient>();
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

        private async Task<Amazon.SQS.Model.Message?> GetSqsMessageAsync()
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

        private QueueMessageContext GetContext(Amazon.SQS.Model.Message sqsMessage)
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
                var topicArn = _dispatchCallbacksCollection.SuccessTopicArn;
                var notificationCallback = _dispatchCallbacksCollection.NotificationDelegateForSuccessfulDispatch;
                await notificationCallback.Invoke(_services, queueMessageContext, topicArn);
                
                var receiptHandle = (queueMessageContext as AwsSqsMessageContext)?.MessageReceiptHandle;
                await DeleteSqsMessageAsync(receiptHandle);
            }

            if (dispatchResult == QueueDispatchResult.Failed)
            {
                var topicArn = _dispatchCallbacksCollection.FailureTopicArn;
                var notificationCallback = _dispatchCallbacksCollection.NotificationDelegateForFailedDispatch;
                await notificationCallback.Invoke(_services, queueMessageContext, topicArn);
            }
        }

        private async Task DeleteSqsMessageAsync(string? receiptHandle)
        {
            // Delete the received message from the queue.
            var deleteMessageRequest = new DeleteMessageRequest
            {
                QueueUrl = _queueUrl,
                ReceiptHandle = receiptHandle
            };
            var deleteMessageResponse = await _sqsClient.DeleteMessageAsync(deleteMessageRequest);
            deleteMessageResponse.Validate();
        }

        //private async Task<bool> UnprocessedMessagesExist()
        //{
        //    var request = new GetQueueAttributesRequest
        //    {
        //        QueueUrl = _queueSettings.QueueUrl,
        //        AttributeNames = _pollingAttributes,
        //    };
        //    var response = await _sqsClient.GetQueueAttributesAsync(request);

        //    if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
        //    {
        //        return response.ApproximateNumberOfMessages > 0;
        //    }

        //    _logger.LogWarning($"Failed to poll queue. HttpStatusCode: {response.HttpStatusCode}");
        //    return false;
        //}

        //private async Task ConsumeMessages()
        //{
        //    _logger.LogInformation("Receiving message from SQS");

        //    // Receive a single message from the queue.
        //    var receiveMessageRequest = new ReceiveMessageRequest
        //    {
        //        AttributeNames = { "SentTimestamp" },
        //        MaxNumberOfMessages = _queueSettings.MaxNumberOfMessages,
        //        MessageAttributeNames = { "All" },
        //        QueueUrl = _queueSettings.QueueUrl,
        //        VisibilityTimeout = 0,
        //        WaitTimeSeconds = 0,
        //    };
        //    var receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest);
        //    // TODO: check status code

        //    await DispatchMessages(receiveMessageResponse.Messages);
        //    await DeleteMessages(receiveMessageResponse.Messages); // TODO: delete only after successful dispatch?
        //}
    }
}
