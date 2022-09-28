using CoreWCF.Queue.Common;

namespace AWS.CoreWCF.Server.SQS.CoreWCF.DispatchCallbacks;

public class DispatchCallbacksCollection : IDispatchCallbacksCollection
{
    public string SuccessTopicArn { get; set; }
    public string FailureTopicArn { get; set; }
    public DispatchCallbacks.NotificationDelegate NotificationDelegateForSuccessfulDispatch { get; set; }
    public DispatchCallbacks.NotificationDelegate NotificationDelegateForFailedDispatch { get; set; }

    public DispatchCallbacksCollection()
    {
        SuccessTopicArn = string.Empty;
        FailureTopicArn = string.Empty;
        NotificationDelegateForSuccessfulDispatch = DispatchCallbacks.NullCallback;
        NotificationDelegateForFailedDispatch = DispatchCallbacks.NullCallback;
    }

    public DispatchCallbacksCollection(
        string successTopicArn, 
        string failureTopicArn,
        Func<IServiceProvider, QueueMessageContext, string, Task> notificationFuncForSuccessfulDispatch,
        Func<IServiceProvider, QueueMessageContext, string, Task> notificationFuncForFailedDispatch)
    {
        SuccessTopicArn = successTopicArn;
        FailureTopicArn = failureTopicArn;
        NotificationDelegateForSuccessfulDispatch = new DispatchCallbacks.NotificationDelegate(notificationFuncForSuccessfulDispatch);
        NotificationDelegateForFailedDispatch = new DispatchCallbacks.NotificationDelegate(notificationFuncForFailedDispatch);
    }
}