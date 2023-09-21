using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.ServiceModel;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using AWS.Extensions.PerformanceTests.Common;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;

namespace AWS.Extensions.PerformanceTests
{
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    [SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 2)]
    [ExcludeFromCodeCoverage]
    public class ServerPerformanceTests
    {
        private IWebHost? _host;
        private IAmazonSQS? _setupSqsClient;
        private readonly string _queueName = $"{nameof(ServerPerformanceTests)}-{DateTime.Now.Ticks}";
        private string _queueUrl = "";

        [Params(1, 8, 16)]
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
                {
                    QueueName = _queueName
                }
            );

            #endregion

            #region Pre Saturate Queue

            var message = $"{_queueName}-Message";

            var rawMessage = ClientMessageGenerator.BuildRawClientMessage<ILoggingService>(
                _queueUrl,
                loggingClient => loggingClient.LogMessage(message)
            );

            for (var j = 0; j < 100; j++)
            {
                var batchMessages = Enumerable
                    .Range(0, 10)
                    .Select(_ => new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), rawMessage))
                    .ToList();

                await _setupSqsClient!.SendMessageBatchAsync(_queueUrl, batchMessages);
            }

            Console.WriteLine("Queue Saturation Complete");
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

    public static class ClientMessageGenerator
    {
        public static string BuildRawClientMessage<TContract>(string queueUrl, Action<TContract> clientAction)
            where TContract : class
        {
            var fakeQueueName = "fake";
            var mockSqs = Substitute.For<IAmazonSQS>();

            // intercept the call the client will make to SendMessageAsync and capture the SendMessageRequest
            SendMessageRequest? capturedSendMessageRequest = null;

            mockSqs
                .SendMessageAsync(
                    Arg.Do<SendMessageRequest>(r =>
                    {
                        capturedSendMessageRequest = r;
                    }),
                    Arg.Any<CancellationToken>()
                )
                .Returns(Task.FromResult(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK }));

            mockSqs
                .GetQueueUrlAsync(Arg.Any<string>())
                .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));

            var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(mockSqs, fakeQueueName);
            var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
            var factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
            var channel = factory.CreateChannel();
            ((System.ServiceModel.Channels.IChannel)channel).Open();

            var client = (TContract)channel;

            clientAction.Invoke(client);

            return capturedSendMessageRequest?.MessageBody ?? "";
        }
    }
}
