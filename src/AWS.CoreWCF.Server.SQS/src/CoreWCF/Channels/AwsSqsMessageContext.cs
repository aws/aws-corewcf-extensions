using CoreWCF.Queue.Common;

namespace CoreWCF.Channels;

public class AwsSqsMessageContext : QueueMessageContext
{
    public string? MessageReceiptHandle { get; set; }
}