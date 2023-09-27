using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using Amazon.SQS;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using BenchmarkDotNet.Attributes;

namespace AWS.Extensions.PerformanceTests
{
    /// <inheritdoc cref="Program"/>
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    [SimpleJob(launchCount: 1, warmupCount: 0, iterationCount: 2)]
    [ExcludeFromCodeCoverage]
    public class ClientPerformanceTests
    {
        [Params(2, 4, 8)]
        public int Threads { get; set; }

        private ILoggingService[] _clients = Array.Empty<ILoggingService>();
        private IAmazonSQS? _setupSqsClient;
        private readonly string _queueName = $"{nameof(ClientPerformanceTests)}-{DateTime.Now.Ticks}";

        [GlobalSetup]
        public async Task CreateAllClients()
        {
            _setupSqsClient = new AmazonSQSClient();

            await _setupSqsClient.CreateQueueAsync(_queueName);

            Console.WriteLine($"QueueName: {_queueName}");

            _clients = Enumerable
                .Range(0, Threads)
                .AsParallel()
                .Select(x =>
                {
                    Console.WriteLine($"Creating Client [{x}]");
                    var sqsClient = new AmazonSQSClient();

                    var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(sqsClient, _queueName);
                    var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
                    var factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
                    var channel = factory.CreateChannel();
                    ((System.ServiceModel.Channels.IChannel)channel).Open();

                    return (channel as ILoggingService);
                })
                .ToArray();

            Console.WriteLine("Created all Clients");
        }

        [GlobalCleanup]
        public async Task CleanUp()
        {
            foreach (var client in _clients)
            {
                try
                {
                    (client as System.ServiceModel.Channels.IChannel)?.Close();
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch { }
            }

            var queueUrlDetails = await _setupSqsClient!.GetQueueUrlAsync(_queueName);
            await _setupSqsClient.DeleteQueueAsync(queueUrlDetails.QueueUrl);
        }

        [Benchmark]
        public async Task ClientCanWrite1000Messages()
        {
            var numberOfMessagesPerThread = 1000 / Threads;

            var constMessage = $"Client Perf Message: {DateTime.Now.Ticks}";

            Console.WriteLine($"Begin sending message: [{constMessage}]");

            var tasks = Enumerable
                .Range(0, Threads)
                .Select(
                    id =>
                        Task.Factory.StartNew(() =>
                        {
                            var client = _clients[id];

                            for (var i = 0; i < numberOfMessagesPerThread; i++)
                            {
                                client.LogMessage(constMessage);
                            }

                            Console.WriteLine($"Client [{id}] has completed sending all messages");
                        })
                );

            await Task.WhenAll(tasks);
        }
    }
}
