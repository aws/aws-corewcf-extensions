using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using AWS.Extensions.PerformanceTests.Common;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;

namespace AWS.Extensions.PerformanceTests
{
    /// <inheritdoc cref="Program"/>
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    [SimpleJob(launchCount: 0, warmupCount: 0, iterationCount: 1)]
    [ExcludeFromCodeCoverage]
    public class ServerSingleClientPerformanceTests
    {
        private IWebHost? _host;
        private IAmazonSQS? _setupSqsClient;
        private readonly string _queueName = $"{nameof(ServerSingleClientPerformanceTests)}-{DateTime.Now.Ticks}";
        private string _queueUrl = "";

        [Params(1, 4, 8)]
        public int Threads { get; set; }

        [GlobalSetup]
        public async Task CreateInfrastructure()
        {
            _setupSqsClient = new AmazonSQSClient();

            await _setupSqsClient.CreateQueueAsync(_queueName);
            _queueUrl = (await _setupSqsClient!.GetQueueUrlAsync(_queueName))?.QueueUrl ?? "";

            Console.WriteLine($"QueueName: {_queueName}");
        }

        [IterationSetup]
        public void Setup()
        {
            LoggingService.LogResults.Clear();

            StartupHost().Wait();
        }

        public async Task StartupHost()
        {
            Console.WriteLine($"Begin {nameof(StartupHost)}");

            #region Configure Host

            _host = ServerFactory.StartServer<LoggingService, ILoggingService>(
                _queueName,
                _queueUrl,
                new AWS.CoreWCF.Extensions.SQS.Channels.AwsSqsBinding(concurrencyLevel: Threads)
            );

            #endregion

            #region Pre Saturate Queue

            await ClientMessageGenerator.SaturateQueue(_setupSqsClient, _queueName, _queueUrl);

            #endregion
        }

        [IterationCleanup]
        public void CleanupHost()
        {
            _host?.Dispose();
        }

        [GlobalCleanup]
        public async Task CleanUp()
        {
            await _setupSqsClient!.DeleteQueueAsync(_queueUrl);
        }

        [Benchmark]
        public async Task ServerCanProcess1000Messages()
        {
            var maxTime = TimeSpan.FromMinutes(5);

            var cancelToken = new CancellationTokenSource(maxTime).Token;

            // start the server
            await _host!.StartAsync(cancelToken);

            // wait for server to process all messages
            while (LoggingService.LogResults.Count < 1000)
            {
                Console.WriteLine($"Processed [{LoggingService.LogResults.Count}] Messages");

                await Task.Delay(TimeSpan.FromMilliseconds(250), cancelToken);
            }

            Console.WriteLine($"Processed [{LoggingService.LogResults.Count}] messages");
        }
    }
}
