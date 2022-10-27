using CoreWCF.Queue.Common;

namespace AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;

public class DispatchCallbacksCollection : IDispatchCallbacksCollection
{
    public NotificationDelegate NotificationDelegateForSuccessfulDispatch { get; set; }
    public NotificationDelegate NotificationDelegateForFailedDispatch { get; set; }

    public DispatchCallbacksCollection()
    {
        NotificationDelegateForSuccessfulDispatch = DispatchCallbackFactory.GetNullCallback();
        NotificationDelegateForFailedDispatch = DispatchCallbackFactory.GetNullCallback();
    }

    public DispatchCallbacksCollection(
        Func<IServiceProvider, QueueMessageContext, Task> notificationFuncForSuccessfulDispatch,
        Func<IServiceProvider, QueueMessageContext, Task> notificationFuncForFailedDispatch)
    {
        NotificationDelegateForSuccessfulDispatch = new NotificationDelegate(notificationFuncForSuccessfulDispatch);
        NotificationDelegateForFailedDispatch = new NotificationDelegate(notificationFuncForFailedDispatch);
    }

    public DispatchCallbacksCollection(
        NotificationDelegate delegateForSuccessfulDispatch,
        NotificationDelegate delegateForFailedDispatch)
    {
        NotificationDelegateForSuccessfulDispatch = delegateForSuccessfulDispatch;
        NotificationDelegateForFailedDispatch = delegateForFailedDispatch;
    }
}

public class DispatchCallbacksCollectionFactory
{
    public static IDispatchCallbacksCollection GetDefaultCallbacksCollectionWithSns(string successTopicArn, string failureTopicArn)
    {
        return new DispatchCallbacksCollection(
            DispatchCallbackFactory.GetDefaultSuccessNotificationCallbackWithSns(successTopicArn),
            DispatchCallbackFactory.GetDefaultFailureNotificationCallbackWithSns(failureTopicArn));
    }
    public static IDispatchCallbacksCollection GetNullCallbacksCollection()
    {
        return new DispatchCallbacksCollection();
    }
}