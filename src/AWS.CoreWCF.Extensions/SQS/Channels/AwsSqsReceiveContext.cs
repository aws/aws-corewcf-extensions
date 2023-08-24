using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using CoreWCF.Channels;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

internal class AwsSqsReceiveContext : ReceiveContext
{
    private readonly IServiceProvider _services;

    private readonly IDispatchCallbacksCollection _dispatchCallbacksCollection;
    private readonly SQSMessageProvider _sqsMessageProvider;
    private readonly string _queueName;
    private readonly AwsSqsMessageContext _sqsMessageContext;

    public AwsSqsReceiveContext(
        IServiceProvider services,
        IDispatchCallbacksCollection dispatchCallbacksCollection,
        SQSMessageProvider sqsMessageProvider,
        string queueName,
        AwsSqsMessageContext sqsMessageContext
    )
    {
        _services = services;
        _dispatchCallbacksCollection = dispatchCallbacksCollection;
        _sqsMessageProvider = sqsMessageProvider;
        _queueName = queueName;
        _sqsMessageContext = sqsMessageContext;
    }

    protected override async Task OnCompleteAsync(CancellationToken token)
    {
        var notificationCallback = _dispatchCallbacksCollection.NotificationDelegateForSuccessfulDispatch;
        await notificationCallback.Invoke(_services, _sqsMessageContext);

        await _sqsMessageProvider.DeleteSqsMessageAsync(_queueName, _sqsMessageContext.MessageReceiptHandle);
    }

    protected override async Task OnAbandonAsync(CancellationToken token)
    {
        var notificationCallback = _dispatchCallbacksCollection.NotificationDelegateForFailedDispatch;
        await notificationCallback.Invoke(_services, _sqsMessageContext);
    }
}
