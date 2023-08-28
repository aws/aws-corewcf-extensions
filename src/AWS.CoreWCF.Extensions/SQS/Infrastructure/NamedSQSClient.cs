using Amazon.SQS;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

internal class NamedSQSClient
{
    public string? QueueName { get; set; }
    public IAmazonSQS? SQSClient { get; set; }
}

internal class NamedSQSClientCollection : List<NamedSQSClient>
{
    public NamedSQSClientCollection(IEnumerable<NamedSQSClient> items)
        : base(items) { }
}
