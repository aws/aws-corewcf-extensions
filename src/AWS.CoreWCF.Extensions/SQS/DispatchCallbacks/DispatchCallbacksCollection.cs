using CoreWCF.Queue.Common;

namespace AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;

public class DispatchCallbacksCollection : IDispatchCallbacksCollection
{
    public NotificationDelegate NotificationDelegateForSuccessfulDispatch { get; set; }
    public NotificationDelegate NotificationDelegateForFailedDispatch { get; set; }

    public DispatchCallbacksCollection(
        Func<IServiceProvider, QueueMessageContext, Task>? successfulDispatch = null,
        Func<IServiceProvider, QueueMessageContext, Task>? failedDispatch = null
    )
    {
        successfulDispatch ??= (_, _) => Task.CompletedTask;
        failedDispatch ??= (_, _) => Task.CompletedTask;

        NotificationDelegateForSuccessfulDispatch = new NotificationDelegate(successfulDispatch);
        NotificationDelegateForFailedDispatch = new NotificationDelegate(failedDispatch);
    }

    public DispatchCallbacksCollection(
        NotificationDelegate delegateForSuccessfulDispatch,
        NotificationDelegate delegateForFailedDispatch
    )
    {
        NotificationDelegateForSuccessfulDispatch = delegateForSuccessfulDispatch;
        NotificationDelegateForFailedDispatch = delegateForFailedDispatch;
    }
}

public class DispatchCallbacksCollectionFactory
{
    public static IDispatchCallbacksCollection GetDefaultCallbacksCollectionWithSns(
        string successTopicArn,
        string failureTopicArn
    )
    {
        return new DispatchCallbacksCollection(
            DispatchCallbackFactory.GetDefaultSuccessNotificationCallbackWithSns(successTopicArn),
            DispatchCallbackFactory.GetDefaultFailureNotificationCallbackWithSns(failureTopicArn)
        );
    }
}
