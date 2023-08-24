using System.Diagnostics.CodeAnalysis;
using Amazon.Extensions.NETCore.Setup;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.Extensions.IntegrationTests.Common;
using AWS.Extensions.IntegrationTests.SQS.TestHelpers;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using CoreWCF.Queue.Common;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

[Collection("ClientAndServer collection")]
[ExcludeFromCodeCoverage]
public class ClientAndServerIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ClientAndServerFixture _clientAndServerFixture;

    public ClientAndServerIntegrationTests(ITestOutputHelper output, ClientAndServerFixture clientAndServerFixture)
    {
        _output = output;
        _clientAndServerFixture = clientAndServerFixture;
    }

    [Fact]
    public async Task ServerReadsAndDispatchesMessageFromSqs()
    {
        // ARRANGE
        var successfulDispatchCallbackWasInvoked = false;
        var callbacks = new DispatchCallbacksCollection(
            new Func<IServiceProvider, QueueMessageContext, Task>(
                (_, _) =>
                {
                    successfulDispatchCallbackWasInvoked = true;
                    return Task.CompletedTask;
                }
            ),
            (_, _) => Task.CompletedTask
        );

        _clientAndServerFixture.Start(_output, dispatchCallbacks: callbacks);

        var clientService = _clientAndServerFixture.Channel!;
        var sqsClient = _clientAndServerFixture.SqsClient!;
        var queueName = ClientAndServerFixture.QueueWithDefaultSettings;

        // make sure queue is starting empty
        await SqsAssert.QueueIsEmpty(sqsClient, queueName);

        var expectedLogMessage = nameof(ServerReadsAndDispatchesMessageFromSqs) + Guid.NewGuid();

        // ACT
        clientService.LogMessage(expectedLogMessage);

        var serverReceivedMessage = false;

        // poll for up to 20 seconds
        for (var polling = 0; polling < 40; polling++)
        {
            if (LoggingService.LogResults.Contains(expectedLogMessage))
            {
                serverReceivedMessage = true;
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        // ASSERT
        Assert.True(serverReceivedMessage);

        Assert.True(successfulDispatchCallbackWasInvoked);

        await SqsAssert.QueueIsEmpty(sqsClient, queueName);
    }

    [Fact]
    public async Task ServiceFaultTriggersFailureCallback()
    {
        // ARRANGE
        var queueName = nameof(ServiceFaultTriggersFailureCallback) + Guid.NewGuid();

        var failureDispatchCallbackWasInvoked = false;
        var callbacks = new DispatchCallbacksCollection(
            new Func<IServiceProvider, QueueMessageContext, Task>((_, _) => Task.CompletedTask),
            (_, _) =>
            {
                failureDispatchCallbackWasInvoked = true;
                return Task.CompletedTask;
            }
        );

        _clientAndServerFixture.Start(_output, queueName, callbacks);

        var clientService = _clientAndServerFixture.Channel!;

        // ACT
        clientService.CauseFailure();

        // poll for up to 20 seconds
        for (var polling = 0; polling < 40; polling++)
        {
            if (failureDispatchCallbackWasInvoked)
                break;

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        // ASSERT
        try
        {
            Assert.True(failureDispatchCallbackWasInvoked);
        }
        finally
        {
            var queueUrlResponse = await _clientAndServerFixture.SqsClient!.GetQueueUrlAsync(queueName);
            await _clientAndServerFixture.SqsClient.DeleteQueueAsync(queueUrlResponse.QueueUrl);
        }
    }

    [Fact]
    public async Task CanCreateQueue()
    {
        _clientAndServerFixture.Start(_output);

        var sqsClient = _clientAndServerFixture.SqsClient!;

        var queueName = nameof(CanCreateQueue) + Guid.NewGuid();

        var createQueueRequest = new CreateQueueRequest(queueName)
            .WithDeadLetterQueue()
            .WithKMSEncryption("kmsMasterKeyId");

        await sqsClient.EnsureSQSQueue(createQueueRequest);

        var queueUrlResult = await sqsClient.GetQueueUrlAsync(queueName);

        Assert.False(string.IsNullOrEmpty(queueUrlResult?.QueueUrl));

        // clean up
        await sqsClient.DeleteQueueAsync(queueUrlResult.QueueUrl);
        await sqsClient.DeleteQueueAsync($"{queueUrlResult.QueueUrl}-DLQ");
    }

    public void Dispose()
    {
        try
        {
            _clientAndServerFixture.SqsClient
                ?.ClearQueues(
                    ClientAndServerFixture.FifoQueueName,
                    ClientAndServerFixture.QueueWithDefaultSettings,
                    ClientAndServerFixture.SnsNotificationSuccessQueue
                )
                .Wait();
        }
        catch { }

        _clientAndServerFixture.Dispose();
    }
}
