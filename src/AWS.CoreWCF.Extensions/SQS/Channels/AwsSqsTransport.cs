using System.IO.Pipelines;
using System.Text;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AWS.CoreWCF.Extensions.SQS.Channels;

internal class AwsSqsTransport : IQueueTransport
{
    private readonly ILogger<AwsSqsTransport> _logger;

    private readonly IServiceProvider _services;
    private readonly Uri _baseAddress;
    private readonly string _queueName;
    private readonly Encoding _encoding;
    private readonly IDispatchCallbacksCollection _dispatchCallbacksCollection;
    private readonly SQSMessageProvider _sqsMessageProvider;

    public int ConcurrencyLevel { get; }

    public AwsSqsTransport(
        IServiceProvider services,
        IServiceDispatcher serviceDispatcher,
        string queueName,
        Encoding encoding,
        IDispatchCallbacksCollection dispatchCallbacksCollection,
        ILogger<AwsSqsTransport> logger,
        int concurrencyLevel = 1
    )
    {
        _services = services;
        _baseAddress = serviceDispatcher.BaseAddress;
        _queueName = queueName;
        _encoding = encoding;
        ConcurrencyLevel = concurrencyLevel;
        _dispatchCallbacksCollection = dispatchCallbacksCollection;
        _logger = logger;
        _sqsMessageProvider = _services.GetRequiredService<SQSMessageProvider>();
    }

    public async ValueTask<QueueMessageContext?> ReceiveQueueMessageContextAsync(CancellationToken cancellationToken)
    {
        try
        {
            var sqsMessage = await _sqsMessageProvider.ReceiveMessageAsync(_queueName);

            if (sqsMessage is null)
            {
                return null;
            }

            var queueMessageContext = GetContext(sqsMessage);
            return queueMessageContext;
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, $"Failed starting Queue Message Context: {e.Message}");

            throw;
        }
    }

    private QueueMessageContext GetContext(Message sqsMessage)
    {
        var reader = PipeReader.Create(sqsMessage.Body.ToStream(_encoding));
        var receiptHandle = sqsMessage.ReceiptHandle;

        var context = new AwsSqsMessageContext
        {
            QueueMessageReader = reader,
            LocalAddress = new EndpointAddress(_baseAddress),
            MessageReceiptHandle = receiptHandle
        };

        context.ReceiveContext = new AwsSqsReceiveContext(
            _services,
            _dispatchCallbacksCollection,
            _sqsMessageProvider,
            _queueName,
            context
        );

        return context;
    }
}
