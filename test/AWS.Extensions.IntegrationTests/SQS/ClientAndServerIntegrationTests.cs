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
    public async Task Server_Reads_And_Dispatches_Message_From_Sqs()
    {
        var successfulDispatchCallbackWasInvoked = false;
        var callbacks = new DispatchCallbacksCollection(
            new Func<IServiceProvider, QueueMessageContext, Task>((_, _) =>
            {
                successfulDispatchCallbackWasInvoked = true; 
                return Task.CompletedTask;
            }),
            (_, _) => Task.CompletedTask);
         
        _clientAndServerFixture.Start(_output, callbacks);

        var clientService = _clientAndServerFixture.Channel;
        var sqsClient = _clientAndServerFixture.SqsClient;
        var queueName = ClientAndServerFixture.QueueWithDefaultSettings;

        // make sure queue is starting empty
        await SqsAssert.QueueIsEmpty(sqsClient, queueName);

        var expectedLogMessage = nameof(Server_Reads_And_Dispatches_Message_From_Sqs) + Guid.NewGuid();
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

        Assert.True(serverReceivedMessage);
        Assert.True(successfulDispatchCallbackWasInvoked);

        await SqsAssert.QueueIsEmpty(sqsClient, queueName);
    }

    [Fact]
    public async Task Can_Create_Queue()
    {
        _clientAndServerFixture.Start(_output);

        var sqsClient = _clientAndServerFixture.SqsClient!;

        var queueName = nameof(Can_Create_Queue) + Guid.NewGuid();

        var fakeAwsAccountToAllow = "123456789010";

        var awsOptions = new AWSOptions();
        _clientAndServerFixture.AWSOptionsBuilder.Populate(awsOptions);

        var createQueueRequest =
            new CreateQueueRequest(queueName)
                .WithDeadLetterQueue()
                .WithKMSEncryption("kmsMasterKeyId");

        await sqsClient.EnsureSQSQueue(awsOptions, createQueueRequest, new []{ fakeAwsAccountToAllow });

        var queueUrlResult = await sqsClient.GetQueueUrlAsync(queueName);

        Assert.False(string.IsNullOrEmpty(queueUrlResult?.QueueUrl));

        // clean up
        await sqsClient.DeleteQueueAsync(queueUrlResult.QueueUrl);
        await sqsClient.DeleteQueueAsync($"{queueUrlResult.QueueUrl}-DLQ");
    }

    public void Dispose()
    {
        _clientAndServerFixture.Dispose();
    }
}
