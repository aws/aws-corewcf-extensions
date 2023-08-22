namespace AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;

public interface IDispatchCallbacksCollection
{
    public NotificationDelegate? NotificationDelegateForSuccessfulDispatch { get; set; }
    public NotificationDelegate? NotificationDelegateForFailedDispatch { get; set; }
}
