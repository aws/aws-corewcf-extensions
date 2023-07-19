using CoreWCF.Queue.Common;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

public class AwsSqsMessageContext : QueueMessageContext
{
    public string? MessageReceiptHandle { get; set; }
}
