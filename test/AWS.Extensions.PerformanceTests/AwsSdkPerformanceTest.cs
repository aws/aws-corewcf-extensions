using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Extensions.PerformanceTests.Common;
using BenchmarkDotNet.Attributes;

namespace AWS.Extensions.PerformanceTests
{
    /// <inheritdoc cref="Program"/>
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    [SimpleJob(launchCount: 0, warmupCount: 0, iterationCount: 1)]
    [ExcludeFromCodeCoverage]
    public class AwsSdkPerformanceTest
    {
        private IAmazonSQS? _setupSqsClient;

        private readonly string _queueName = $"{nameof(AwsSdkPerformanceTest)}-{DateTime.Now.Ticks}";
        private string _queueUrl = "";

        private AmazonSQSClient[] _clientPool = Array.Empty<AmazonSQSClient>();

        private string _fakeQueueMessage = string.Empty;

        [Params(1, 2, 4)]
        public int Threads { get; set; }

        [GlobalSetup]
        public async Task CreateInfrastructure()
        {
            _setupSqsClient = new AmazonSQSClient();

            await _setupSqsClient.CreateQueueAsync(_queueName);
            _queueUrl = (await _setupSqsClient!.GetQueueUrlAsync(_queueName))?.QueueUrl ?? "";

            Console.WriteLine($"QueueName: {_queueName}");

            _clientPool = Enumerable.Range(0, 4).Select(_ => new AmazonSQSClient()).ToArray();

            _fakeQueueMessage = _queueName;

            // write an extra 5000 messages so the queue is nicely saturated
            // and ReadAndWriteMessages has extra messages it can read if necessary
            await ClientMessageGenerator.SaturateQueue(_setupSqsClient, _queueName, _queueUrl, numMessages: 5000);
        }

        [GlobalCleanup]
        public async Task CleanUp()
        {
            await _setupSqsClient!.DeleteQueueAsync(_queueUrl);
        }

        [Benchmark]
        public async Task ReadAndWrite100Messages()
        {
            var cancelToken = new CancellationTokenSource(TimeSpan.FromMinutes(2)).Token;

            var sw = Stopwatch.StartNew();

            var tasks = Enumerable
                .Range(0, Threads)
                .Select(i => ReadAndWriteMessages(_clientPool[i], 100 / Threads, cancelToken))
                .ToArray();

            await Task.WhenAll(tasks);

            Console.WriteLine("=======================");
            Console.WriteLine($"Sent & Read 100 Messages in [{sw.Elapsed.TotalSeconds}] s");
            Console.WriteLine("=======================");
        }

        private async Task ReadAndWriteMessages(IAmazonSQS client, int numberOfMessages, CancellationToken cancelToken)
        {
            Console.WriteLine($"[T {Thread.CurrentThread.ManagedThreadId}] Begin Writing Messages");
            for (var i = 0; i < numberOfMessages; i++)
            {
                await client.SendMessageAsync(_queueUrl, _fakeQueueMessage, cancelToken);
            }

            Console.WriteLine($"[T {Thread.CurrentThread.ManagedThreadId}] Begin Reading Messages");

            var messagesRead = 0;
            while (messagesRead < numberOfMessages)
            {
                var request = new ReceiveMessageRequest { QueueUrl = _queueUrl, MaxNumberOfMessages = 10 };
                var response = await client.ReceiveMessageAsync(request, cancelToken);

                foreach (var msg in response.Messages)
                    await client.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, cancelToken);

                messagesRead += response.Messages.Count;
            }
        }
    }
}
