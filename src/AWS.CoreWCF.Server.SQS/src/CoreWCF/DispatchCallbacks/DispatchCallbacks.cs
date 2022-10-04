using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AWS.CoreWCF.Server.Common;
using CoreWCF.Channels;
using CoreWCF.Queue.Common;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.CoreWCF.Server.SQS.CoreWCF.DispatchCallbacks;

public class DispatchCallbacks
{
    public delegate Task NotificationDelegate(IServiceProvider services, QueueMessageContext context, string topicArn);

    public static async Task NullCallback(IServiceProvider services, QueueMessageContext context, string topicArn)
    { }

    public static async Task DefaultSuccessNotificationCallbackWithSns(IServiceProvider services, QueueMessageContext context, string topicArn)
    {
        var subject = "Message Dispatch Successful";
        var sqsContext = context as AwsSqsMessageContext;
        var message = sqsContext is null 
            ? $"{nameof(QueueMessageContext)} of type {nameof(AwsSqsMessageContext)} was expected but type of {context.GetType()} was received." 
            : $"Succeeded to dispatch message to {sqsContext.LocalAddress} with receipt {sqsContext.MessageReceiptHandle}";
        
        var publishRequest = new PublishRequest
        {
            TargetArn = topicArn,
            Subject = subject,
            Message = message,
            MessageGroupId = subject.ToCrc32Hash(),
            MessageDeduplicationId = message.ToCrc32Hash()
        };
        await SendNotificationToSns(services, publishRequest);
    }

    public static async Task DefaultFailureNotificationCallbackWithSns(IServiceProvider services, QueueMessageContext context, string topicArn)
    {
        var subject = "Message Dispatch Failed";
        var sqsContext = context as AwsSqsMessageContext;
        var message = sqsContext is null 
            ? $"{nameof(QueueMessageContext)} of type {nameof(AwsSqsMessageContext)} was expected but type of {context.GetType()} was received." 
            : $"Failed to dispatch message to {sqsContext.LocalAddress} with receipt {sqsContext.MessageReceiptHandle}";
        
        var publishRequest = new PublishRequest
        {
            TargetArn = topicArn,
            Subject = subject,
            Message = message,
            MessageGroupId = subject.ToCrc32Hash(),
            MessageDeduplicationId = message.ToCrc32Hash()
        };
        await SendNotificationToSns(services, publishRequest);
    }

    public static async Task SendNotificationToSns(IServiceProvider services, PublishRequest publishRequest)
    {
        try
        {
            var snsClient = services.GetRequiredService<IAmazonSimpleNotificationService>();
            var response = await snsClient.PublishAsync(publishRequest);

            response.Validate();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
