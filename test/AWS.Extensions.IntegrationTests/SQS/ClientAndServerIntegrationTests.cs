using System.Diagnostics.CodeAnalysis;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using AWS.Extensions.IntegrationTests.Common;
using AWS.Extensions.IntegrationTests.SQS.TestHelpers;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using CoreWCF.Queue.Common;
using Shouldly;
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

        AWSConfigs.InitializeCollections = true;
    }

    [Theory]
    [InlineData(ClientAndServerFixture.QueueWithDefaultSettings)]
    [InlineData(ClientAndServerFixture.FifoQueueName)]
    public async Task ServerReadsAndDispatchesMessageFromSqs(string queueName)
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

        _clientAndServerFixture.Start(
            _output,
            queueName: queueName,
            createQueue: null, // standard queues are created via cdk
            dispatchCallbacks: callbacks
        );

        var clientService = _clientAndServerFixture.Channel!;
        var sqsClient = _clientAndServerFixture.SqsClient!;

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

                // small sleep to allow corewcf to complete and fire callbacks
                await Task.Delay(TimeSpan.FromMilliseconds(100));

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
    public async Task FailedMessagesGoToDeadLetterQueue()
    {
        // ARRANGE
        var queueName = nameof(FailedMessagesGoToDeadLetterQueue) + DateTime.Now.Ticks;

        var foundMessageInDeadLetterQueue = false;

        _clientAndServerFixture.Start(
            _output,
            queueName,
            new CreateQueueRequest(queueName)
                .SetDefaultValues()
                .SetAttribute(QueueAttributeName.VisibilityTimeout, "1")
                .WithDeadLetterQueue(maxReceiveCount: 1),
            dispatchCallbacks: new DispatchCallbacksCollection()
        );

        var dlqName = $"{queueName}-DLQ";
        var dlqUrl = (await _clientAndServerFixture.SqsClient!.GetQueueUrlAsync(dlqName)).QueueUrl;

        var clientService = _clientAndServerFixture.Channel!;

        // ACT
        clientService.CauseFailure();

        // poll for up to 20 seconds
        for (var polling = 0; polling < 40; polling++)
        {
            var queueDetails = await _clientAndServerFixture.SqsClient.GetQueueAttributesAsync(
                dlqUrl,
                new List<string> { QueueAttributeName.ApproximateNumberOfMessages }
            );

            foundMessageInDeadLetterQueue = queueDetails.ApproximateNumberOfMessages > 0;

            if (foundMessageInDeadLetterQueue)
                break;

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        // ASSERT
        try
        {
            foundMessageInDeadLetterQueue.ShouldBeTrue();
        }
        finally
        {
            await DeleteQueue(queueName);
        }
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

        _clientAndServerFixture.Start(
            _output,
            queueName,
            new CreateQueueRequest(queueName).SetDefaultValues().WithDeadLetterQueue(),
            callbacks
        );

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
            failureDispatchCallbackWasInvoked.ShouldBeTrue();
        }
        finally
        {
            await DeleteQueue(queueName);
        }
    }

    [Fact]
    public async Task CanCreateQueue()
    {
        var queueName = nameof(CanCreateQueue) + Guid.NewGuid();
        _clientAndServerFixture.Start(
            _output,
            queueName,
            new CreateQueueRequest(queueName).WithDeadLetterQueue().WithKMSEncryption("kmsMasterKeyId")
        );

        var queueUrlResult = await _clientAndServerFixture.SqsClient!.GetQueueUrlAsync(queueName);

        queueUrlResult?.QueueUrl.ShouldNotBeNullOrEmpty();

        // clean up
        await DeleteQueue(queueName);
    }

    [Fact]
    public async Task CanCreateQueueWithManagedServerSideEncryption()
    {
        var queueName = nameof(CanCreateQueue) + Guid.NewGuid();
        _clientAndServerFixture.Start(
            _output,
            queueName,
            new CreateQueueRequest(queueName).WithDeadLetterQueue().WithManagedServerSideEncryption()
        );

        var queueUrlResult = await _clientAndServerFixture.SqsClient!.GetQueueUrlAsync(queueName);

        var queueAttributes = await _clientAndServerFixture.SqsClient.GetQueueAttributesAsync(
            queueUrlResult.QueueUrl,
            new List<string> { QueueAttributeName.SqsManagedSseEnabled }
        );

        queueAttributes.Attributes.ContainsKey(QueueAttributeName.SqsManagedSseEnabled).ShouldBeTrue();
        queueAttributes.Attributes[QueueAttributeName.SqsManagedSseEnabled].ShouldBe("true");

        // clean up
        await DeleteQueue(queueName);
    }

    [Fact]
    public async Task CanCreateFifoQueue()
    {
        var queueName = $"{nameof(CanCreateFifoQueue)}{DateTime.Now.Ticks}.fifo";
        _clientAndServerFixture.Start(
            _output,
            queueName,
            createQueue: new CreateQueueRequest(queueName).SetDefaultValues().WithFIFO().WithDeadLetterQueue()
        );

        var queueUrlResult = await _clientAndServerFixture.SqsClient!.GetQueueUrlAsync(queueName);

        queueUrlResult?.QueueUrl.ShouldNotBeNullOrEmpty();

        // clean up
        await DeleteQueue(queueName);
    }

    private async Task DeleteQueue(string queueName)
    {
        var sqsClient = _clientAndServerFixture.SqsClient!;

        var queueUrlResult = await sqsClient.GetQueueUrlAsync(queueName);

        await sqsClient.DeleteQueueAsync(queueUrlResult.QueueUrl);

        var dlqName = queueName.EndsWith(".fifo") ? $"{queueName.Replace(".fifo", "-DLQ.fifo")}" : $"{queueName}-DLQ";

        try
        {
            var dlqUrlResult = await sqsClient.GetQueueUrlAsync(dlqName);
            await sqsClient.DeleteQueueAsync(dlqUrlResult.QueueUrl);
        }
        catch (QueueDoesNotExistException) { }
    }

    public void Dispose()
    {
        try
        {
            _clientAndServerFixture
                .SqsClient?.ClearQueues(
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
