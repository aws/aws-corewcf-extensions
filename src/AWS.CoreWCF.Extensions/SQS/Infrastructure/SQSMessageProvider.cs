using System.Collections.Concurrent;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.Extensions.Logging;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

/// <remarks>
/// Requires a custom DI Factory.  See <see cref="SQSServiceCollectionExtensions.AddAmazonSQSClient"/>
/// </remarks>
public interface ISQSMessageProvider
{
    Task<Amazon.SQS.Model.Message?> ReceiveMessageAsync(string queueName);
    Task DeleteSqsMessageAsync(string queueName, string receiptHandle);
}

internal class SQSMessageProvider : ISQSMessageProvider
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
            if (null == namedSQSClient?.SQSClient || string.IsNullOrEmpty(namedSQSClient.QueueName))
                throw new ArgumentException($"Invalid [{nameof(NamedSQSClient)}]", nameof(namedSQSClientCollections));

            string queueUrl;

            try
            {
                queueUrl = namedSQSClient.SQSClient.GetQueueUrlAsync(namedSQSClient.QueueName).Result.QueueUrl;
            }
            catch (Exception e)
            {
                throw new ArgumentException(
                    $"Exception loading Queue details for [{namedSQSClient.QueueName}].  "
                        + $"Make sure it has been created.",
                    nameof(namedSQSClientCollections),
                    e
                );
            }

            var entry = new SQSMessageProviderQueueCacheEntry
            {
                QueueName = namedSQSClient.QueueName!,
                QueueUrl = queueUrl,
                SQSClient = namedSQSClient.SQSClient
            };

            _cache.AddOrUpdate(namedSQSClient.QueueName!, _ => entry, (_, _) => entry);
        }
    }

    public async Task<Amazon.SQS.Model.Message?> ReceiveMessageAsync(string queueName)
    {
        if (!_cache.TryGetValue(queueName, out var cacheEntry))
            throw new ArgumentException(
                $"QueueName [{queueName}] was not found.  Was it registered in the Constructor?",
                nameof(queueName)
            );

        var cachedMessages = cacheEntry.QueueMessages;
        var sqsClient = cacheEntry.SQSClient;
        var queueUrl = cacheEntry.QueueUrl;
        var mutex = cacheEntry.Mutex;

        if (cachedMessages.IsEmpty)
        {
            await mutex.WaitAsync().ConfigureAwait(false);

            try
            {
                if (cachedMessages.IsEmpty)
                {
                    var newMessages = await sqsClient.ReceiveMessagesAsync(queueUrl, _logger);
                    foreach (var newMessage in newMessages)
                    {
                        cachedMessages.Enqueue(newMessage);
                    }
                }
            }
            finally
            {
                mutex.Release();
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
            throw new ArgumentException(
                $"QueueName [{queueName}] was not found.  Was it registered in the Constructor?",
                nameof(queueName)
            );

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
