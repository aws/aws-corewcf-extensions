using System.Collections.Concurrent;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public class SQSMessageProvider
{
    private const int MaxSqsBatchSize = 10;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, ConcurrentQueue<Message>> _queueMessageCache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _cacheMutexes;
    private readonly ConcurrentDictionary<string, NamedSQSClient> _namedSQSClients;

    public SQSMessageProvider(IEnumerable<NamedSQSClient> namedSQSClients, ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;

        // TODO: handle visibility timeout if needed later via MemoryCache expiry
        _queueMessageCache = new ConcurrentDictionary<string, ConcurrentQueue<Message>>();
        _cacheMutexes = new ConcurrentDictionary<string, SemaphoreSlim>();
        _namedSQSClients = new ConcurrentDictionary<string, NamedSQSClient>();

        foreach (var namedSQSClient in namedSQSClients)
        {
            var queueName = namedSQSClient.QueueName;
            _queueMessageCache.TryAdd(queueName, new ConcurrentQueue<Message>());
            _cacheMutexes.TryAdd(queueName, new SemaphoreSlim(1, 1));
            _namedSQSClients.TryAdd(queueName, namedSQSClient);
        }
    }

    public async Task<Message?> ReceiveMessageAsync(string queueName)
    {
        var cachedMessages = _queueMessageCache[queueName];
        var namedClient = _namedSQSClients[queueName];
        var queueUrl = namedClient.QueueUrl;

        if (cachedMessages.IsEmpty)
        {
            var mutex = _cacheMutexes[queueName];
            await mutex.WaitAsync().ConfigureAwait(false);
            try
            {
                var newMessages = await namedClient.SQSClient.ReceiveMessagesAsync(queueUrl, _logger, MaxSqsBatchSize);
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

        if (cachedMessages.TryDequeue(out var message))
        {
            return message;
        }

        return null;
    }

    public async Task DeleteSqsMessageAsync(string queueName, string receiptHandle)
    {
        var namedClient = _namedSQSClients[queueName];
        var queueUrl = namedClient.QueueUrl;

        await namedClient.SQSClient.DeleteMessageAsync(queueUrl, receiptHandle, _logger);
    }
}
