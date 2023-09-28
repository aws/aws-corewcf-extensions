using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using AWS.Extensions.PerformanceTests.Common;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;

namespace AWS.Extensions.PerformanceTests;

/// <inheritdoc cref="Program"/>
[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
[SimpleJob(launchCount: 0, warmupCount: 0, iterationCount: 1)]
[ExcludeFromCodeCoverage]
public class ServerMultipleClientsPerformanceTests
{
    private IWebHost? _host;
    private IAmazonSQS? _setupSqsClient;

    private readonly string _queueName1 = $"{nameof(ServerMultipleClientsPerformanceTests)}-1-{DateTime.Now.Ticks}";
    private readonly string _queueName2 = $"{nameof(ServerMultipleClientsPerformanceTests)}-2-{DateTime.Now.Ticks}";
    private readonly string _queueName3 = $"{nameof(ServerMultipleClientsPerformanceTests)}-3-{DateTime.Now.Ticks}";

    private string _queueUrl1 = "";
    private string _queueUrl2 = "";
    private string _queueUrl3 = "";

    [Params(2, 3)]
    public int Clients { get; set; }

    [GlobalSetup]
    public async Task CreateInfrastructure()
    {
        _setupSqsClient = new AmazonSQSClient();

        await _setupSqsClient.CreateQueueAsync(_queueName1);
        _queueUrl1 = (await _setupSqsClient!.GetQueueUrlAsync(_queueName1))?.QueueUrl ?? "";

        await _setupSqsClient.CreateQueueAsync(_queueName2);
        _queueUrl2 = (await _setupSqsClient!.GetQueueUrlAsync(_queueName2))?.QueueUrl ?? "";

        await _setupSqsClient.CreateQueueAsync(_queueName3);
        _queueUrl3 = (await _setupSqsClient!.GetQueueUrlAsync(_queueName3))?.QueueUrl ?? "";

        Console.WriteLine($"QueueNames: {_queueName1}, {_queueName2}, {_queueName3}");
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

        var queueBindingPairs = new List<(string queueName, string queueUrl, AwsSqsBinding binding)>
        {
            (_queueName1, _queueUrl1, new AwsSqsBinding()),
            (_queueName2, _queueUrl2, new AwsSqsBinding()),
            (_queueName3, _queueUrl3, new AwsSqsBinding())
        };

        _host = ServerFactory.StartServer<LoggingService, ILoggingService>(queueBindingPairs.Take(Clients).ToArray());

        #endregion

        #region Pre Saturate Queue

        await Task.WhenAll(
            ClientMessageGenerator.SaturateQueue(_setupSqsClient, _queueName1, _queueUrl1),
            ClientMessageGenerator.SaturateQueue(_setupSqsClient, _queueName2, _queueUrl2),
            ClientMessageGenerator.SaturateQueue(_setupSqsClient, _queueName3, _queueUrl3)
        );

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
        await _setupSqsClient!.DeleteQueueAsync(_queueUrl1);
        await _setupSqsClient!.DeleteQueueAsync(_queueUrl2);
        await _setupSqsClient!.DeleteQueueAsync(_queueUrl3);
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
