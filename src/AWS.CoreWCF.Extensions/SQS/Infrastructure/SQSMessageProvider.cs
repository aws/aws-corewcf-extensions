using System.Collections.Concurrent;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.Extensions.Logging;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

/// <remarks>
/// Requires a custom DI Factory.  See <see cref="SQSServiceCollectionExtensions.AddAmazonSQSClient"/>
/// </remarks>
internal class SQSMessageProvider
{
    private readonly ILogger<SQSMessageProvider> _logger;

    private readonly ConcurrentDictionary<string, SQSMessageProviderQueueCacheEntry> _cache =
        new(StringComparer.InvariantCultureIgnoreCase);

    public SQSMessageProvider(
        IEnumerable<NamedSQSClientCollection> namedSQSClientCollections,
        ILogger<SQSMessageProvider> logger
    )
    {
        _logger = logger;

        var namedSQSClients = namedSQSClientCollections.SelectMany(x => x).ToList();

        foreach (var namedSQSClient in namedSQSClients)
        {
            if (null == namedSQSClient?.SQSClient || null == namedSQSClient?.QueueName)
                throw new ArgumentException($"Invalid [{nameof(NamedSQSClient)}]");

            // TODO: Harden
            var queueUrl = namedSQSClient.SQSClient.GetQueueUrlAsync(namedSQSClient.QueueName).Result.QueueUrl;

            var entry = new SQSMessageProviderQueueCacheEntry
            {
                QueueName = namedSQSClient.QueueName,
                QueueUrl = queueUrl,
                SQSClient = namedSQSClient.SQSClient
            };

            _cache.AddOrUpdate(namedSQSClient.QueueName, _ => entry, (_, _) => entry);
        }
    }

    public async Task<Amazon.SQS.Model.Message?> ReceiveMessageAsync(string queueName)
    {
        if (!_cache.TryGetValue(queueName, out var cacheEntry))
            throw new Exception("Todo");

        var cachedMessages = cacheEntry.QueueMessages;
        var sqsClient = cacheEntry.SQSClient;
        var queueUrl = cacheEntry.QueueUrl;
        var mutex = cacheEntry.Mutex;

        if (cachedMessages.IsEmpty)
        {
            await mutex.WaitAsync().ConfigureAwait(false);

            if (cachedMessages.IsEmpty)
            {
                try
                {
                    var newMessages = await sqsClient.ReceiveMessagesAsync(queueUrl, _logger);
                    foreach (var newMessage in newMessages)
                    {
                        cachedMessages.Enqueue(newMessage);
                    }
                }
                finally
                {
                    mutex.Release();
                }
            }
        }

        if (cachedMessages.TryDequeue(out var message))
        {
            return message;
        }

        return null;
    }

    public async Task DeleteSqsMessageAsync(string queueName, string receiptHandle)
    {
        if (!_cache.TryGetValue(queueName, out var cacheEntry))
            throw new Exception("Todo");

        await cacheEntry.SQSClient.DeleteMessageAsync(cacheEntry.QueueUrl, receiptHandle, _logger);
    }

    private class SQSMessageProviderQueueCacheEntry
    {
        public ConcurrentQueue<Message> QueueMessages { get; } = new();
        public SemaphoreSlim Mutex { get; } = new SemaphoreSlim(1, 1);
        public string QueueName { get; set; }
        public IAmazonSQS SQSClient { get; set; }
        public string QueueUrl { get; set; }
    }
}
