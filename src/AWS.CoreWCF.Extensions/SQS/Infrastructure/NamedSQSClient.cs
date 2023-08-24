using Amazon.SQS;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

internal class NamedSQSClient
{
    public NamedSQSClient(string queueName, IAmazonSQS sqsClient)
    {
        QueueName = queueName;
        SQSClient = sqsClient;
    }

    public string QueueName { get; }
    public IAmazonSQS SQSClient { get; }

    private string? _queueUrl;
    public string? QueueUrl
    {
        get
        {
            if (!this.IsInitialized)
                throw new Exception($"{nameof(NamedSQSClient)} must first be initialize");

            return _queueUrl;
        }
    }

    public bool IsInitialized { get; private set; }

    public async Task Initialize(Func<IAmazonSQS, string, Task>? queueInitializer = null)
    {
        queueInitializer ??= ((_, _) => Task.CompletedTask);

        await queueInitializer(SQSClient, QueueName);

        _queueUrl = (await SQSClient.GetQueueUrlAsync(QueueName)).QueueUrl;

        IsInitialized = true;
    }
}

internal class NamedSQSClientCollection : List<NamedSQSClient>
{
    public NamedSQSClientCollection(IEnumerable<NamedSQSClient> items)
        : base(items) { }
}
