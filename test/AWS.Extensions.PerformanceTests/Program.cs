using System.Diagnostics.CodeAnalysis;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Queue.Common;

namespace AWS.Extensions.PerformanceTests
{
    /// <summary>
    /// Collection of Benchmarking tests to measure the performance of various aspects of the
    /// CoreWCF.SQS extensions for CoreWCF with a special focus on concurrency.
    /// <para />
    /// Concurrency differs between Server and Clients.  Clients can construct multiple
    /// <see cref="System.ServiceModel.Channels.IChannel" /> objects to send messages in parallel.
    /// <see cref="ClientPerformanceTests"/> measures WCF Clients performance by sending messages to SQS, testing
    /// with several values for <see cref="ClientPerformanceTests.Threads"/> to verify increased message throughput
    /// when using multiple  <see cref="System.ServiceModel.Channels.IChannel" /> objects.
    /// <para />
    /// CoreWCF offers two dimensions for Server concurrency.  A Server can listen to multiple queues concurrently by
    /// registering additional clients via <see cref="SQSServiceCollectionExtensions.AddSQSClient"/> and
    /// <see cref="IServiceBuilder.AddServiceEndpoint{TService,TContract}(Binding,string)" />.  This is tested by
    /// <see cref="ServerMultipleClientsPerformanceTests"/>, which registers different numbers of
    /// <see cref="ServerMultipleClientsPerformanceTests.Clients"/> and measures overall Server message throughput.
    /// <para />
    /// The other concurrency dimension for Servers is to set the <see cref="IQueueTransport.ConcurrencyLevel"/>,
    /// which is exposed via the constructor for <see cref="AwsSqsBinding"/>.  This controls the number of Threads
    /// that will ingest a message from SQS and run it through the Service Dispatch pipeline.
    /// _Note_: The CoreWCF pipeline executes the Service Handler on a new thread; changing
    /// <see cref="IQueueTransport.ConcurrencyLevel"/> does NOT NOT impact the number of messages that will be
    /// processed concurrently.  <see cref="ServerSingleClientPerformanceTests"/> tests this by setting
    /// different values of <see cref="IQueueTransport.ConcurrencyLevel"/> to ensure messages ingestion performance
    /// improves with an increase in concurrency.
    /// <para />
    /// Finally <see cref="AwsSdkPerformanceTest"/> performs basic sending and receiving of messages to
    /// Amazon SQS using the SDK directly in order to get a baseline from throughput comparisons.  While this is not an
    /// apples-to-apples comparision with CoreWCF, as CoreWCF has an optimized message pump and processing pipeline, it
    /// provides a ball pack comparision to ensure CoreWCF.SQS performs equally or better than a direct implementation.
    /// </summary>
    /// <remarks>
    /// Example output generated on Sept 27, 2023
    /// // * Summary *
    ///
    /// BenchmarkDotNet v0.13.7, Windows 10 (10.0.19044.3448/21H2/November2021Update)
    /// 11th Gen Intel Core i9-11900H 2.50GHz, 1 CPU, 16 logical and 8 physical cores
    /// .NET SDK 7.0.401
    /// [Host]     : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
    /// Job-JYQWIA : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
    /// Job-EXWFRC : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
    /// Job-YFWFKO : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
    /// Job-ZYQXUQ : .NET 6.0.22 (6.0.2223.42425), X64 RyuJIT AVX2
    ///
    ///
    /// |                                  Type |                       Method |        Job | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Threads | Clients |     Mean | Error |   StdDev |       Gen0 | Exceptions | Completed Work Items | Lock Contentions |       Gen1 |  Allocated |
    /// |-------------------------------------- |----------------------------- |----------- |---------------- |--------------- |------------ |------------- |------------ |-------- |-------- |---------:|------:|---------:|-----------:|-----------:|---------------------:|-----------------:|-----------:|-----------:|
    /// |                ClientPerformanceTests |   ClientCanWrite1000Messages | Job-JYQWIA |         Default |              2 |           1 |           16 |           0 |       2 |       ? | 46.249 s |    NA | 0.4259 s | 75000.0000 |          - |            1136.0000 |                - | 13000.0000 |  903.11 MB |
    /// |                ClientPerformanceTests |   ClientCanWrite1000Messages | Job-JYQWIA |         Default |              2 |           1 |           16 |           0 |       4 |       ? | 22.530 s |    NA | 0.4503 s | 75000.0000 |          - |            1155.0000 |                - | 17000.0000 |  903.05 MB |
    /// |                ClientPerformanceTests |   ClientCanWrite1000Messages | Job-JYQWIA |         Default |              2 |           1 |           16 |           0 |       8 |       ? | 11.464 s |    NA | 0.2899 s | 76000.0000 |          - |            1250.0000 |                - | 20000.0000 |  903.33 MB |
    /// |                 AwsSdkPerformanceTest |      ReadAndWrite100Messages | Job-EXWFRC |         Default |              1 |           0 |           16 |           0 |       1 |       ? | 19.121 s |    NA | 0.0000 s | 15000.0000 |          - |             244.0000 |                - |  3000.0000 |  185.04 MB |
    /// |                 AwsSdkPerformanceTest |      ReadAndWrite100Messages | Job-EXWFRC |         Default |              1 |           0 |           16 |           0 |       2 |       ? |  9.054 s |    NA | 0.0000 s | 15000.0000 |          - |             241.0000 |                - |  3000.0000 |  185.06 MB |
    /// |                 AwsSdkPerformanceTest |      ReadAndWrite100Messages | Job-EXWFRC |         Default |              1 |           0 |           16 |           0 |       4 |       ? |  5.115 s |    NA | 0.0000 s | 17000.0000 |          - |             248.0000 |                - |  4000.0000 |  204.54 MB |
    /// | ServerMultipleClientsPerformanceTests | ServerCanProcess1000Messages | Job-YFWFKO |               1 |              1 |           0 |            1 |           0 |       ? |       2 | 21.645 s |    NA | 0.0000 s | 89000.0000 |          - |            2802.0000 |           1.0000 |  4000.0000 | 1052.95 MB |
    /// | ServerMultipleClientsPerformanceTests | ServerCanProcess1000Messages | Job-YFWFKO |               1 |              1 |           0 |            1 |           0 |       ? |       3 | 15.673 s |    NA | 0.0000 s | 89000.0000 |          - |            2454.0000 |           1.0000 |  3000.0000 | 1051.04 MB |
    /// |    ServerSingleClientPerformanceTests | ServerCanProcess1000Messages | Job-YFWFKO |               1 |              1 |           0 |            1 |           0 |       1 |       ? | 42.874 s |    NA | 0.0000 s | 87000.0000 |          - |            3100.0000 |           4.0000 |  6000.0000 | 1038.63 MB |
    /// |    ServerSingleClientPerformanceTests | ServerCanProcess1000Messages | Job-YFWFKO |               1 |              1 |           0 |            1 |           0 |       4 |       ? | 17.532 s |    NA | 0.0000 s | 90000.0000 |          - |            2846.0000 |           1.0000 |  2000.0000 | 1038.83 MB |
    /// |    ServerSingleClientPerformanceTests | ServerCanProcess1000Messages | Job-YFWFKO |               1 |              1 |           0 |            1 |           0 |       8 |       ? | 14.372 s |    NA | 0.0000 s | 92000.0000 |          - |            3073.0000 |           2.0000 |  5000.0000 | 1037.14 MB |
    ///
    /// // * Legends *
    /// Threads              : Value of the 'Threads' parameter
    /// Clients              : Value of the 'Clients' parameter
    /// Mean                 : Arithmetic mean of all measurements
    /// Error                : Half of 99.9% confidence interval
    /// StdDev               : Standard deviation of all measurements
    /// Gen0                 : GC Generation 0 collects per 1000 operations
    /// Exceptions           : Exceptions thrown per single operation
    /// Completed Work Items : The number of work items that have been processed in ThreadPool (per single operation)
    /// Lock Contentions     : The number of times there was contention upon trying to take a Monitor's lock (per single operation)
    /// Gen1                 : GC Generation 1 collects per 1000 operations
    /// Allocated            : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
    /// 1 s                  : 1 Second (1 sec)
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run(
                typeof(Program).Assembly,
                ManualConfig
                    .Create(DefaultConfig.Instance)
                    .AddValidator(ExecutionValidator.FailOnError)
                    .AddDiagnoser(MemoryDiagnoser.Default, ThreadingDiagnoser.Default, ExceptionDiagnoser.Default)
                    .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Method))
                    .WithOptions(ConfigOptions.JoinSummary)
                    .AddColumn(
                        // Add Custom Columns from running on EC2 Hosts via SSM
                        new TagColumn(
                            "Processor Count",
                            _ => Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS") ?? ""
                        ),
                        new TagColumn(
                            "Instance ID",
                            _ => Environment.GetEnvironmentVariable("AWS_SSM_INSTANCE_ID") ?? ""
                        ),
                        new TagColumn("Region", _ => Environment.GetEnvironmentVariable("AWS_SSM_REGION_NAME") ?? ""),
                        new TagColumn(
                            "EC2 Instance Type",
                            _ => Environment.GetEnvironmentVariable("EC2_INSTANCE_TYPE") ?? ""
                        )
                    )
            );
        }
    }
}
