namespace AWS.CoreWCF.Server.SQS.CoreWCF.DispatchCallbacks;

public interface IDispatchCallbacksCollection
{
    public string SuccessTopicArn { get; set; }
    public string FailureTopicArn { get; set; }
    public DispatchCallbacks.NotificationDelegate NotificationDelegateForSuccessfulDispatch { get; set; }
    public DispatchCallbacks.NotificationDelegate NotificationDelegateForFailedDispatch { get; set; }
}