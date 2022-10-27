using Amazon.SQS;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public class NamedSQSClient
{
    public string QueueName { get; }
    public IAmazonSQS SQSClient { get; }
    public string QueueUrl { get; }

    public NamedSQSClient(string queueName, IAmazonSQS sqsClient)
    {
        QueueName = queueName;
        SQSClient = sqsClient;
        QueueUrl = SQSClient.GetQueueUrlAsync(QueueName).Result.QueueUrl;
    }
}
