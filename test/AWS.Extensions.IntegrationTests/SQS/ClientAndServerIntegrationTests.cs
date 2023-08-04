using AWS.Extensions.IntegrationTests.Common;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

[Collection("ClientAndServer collection")]
public class ClientAndServerIntegrationTests
{
    private readonly ClientAndServerFixture _clientAndServerFixture;

    public ClientAndServerIntegrationTests(ITestOutputHelper output, ClientAndServerFixture clientAndServerFixture)
    {
        _clientAndServerFixture = clientAndServerFixture;

        _clientAndServerFixture.Start(output);
    }

    [Fact]
    public async Task Server_Reads_And_Dispatches_Message_From_Sqs()
    {
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
        await SqsAssert.QueueIsEmpty(sqsClient, queueName);
    }
}
