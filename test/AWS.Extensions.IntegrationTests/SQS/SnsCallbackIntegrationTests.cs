using AWS.Extensions.IntegrationTests.SQS.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

[Collection("ClientAndServer collection")]
public class SnsCallbackIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ClientAndServerFixture _clientAndServerFixture;

    public SnsCallbackIntegrationTests(ITestOutputHelper output, ClientAndServerFixture clientAndServerFixture)
    {
        _output = output;
        _clientAndServerFixture = clientAndServerFixture;
    }

    /// <summary>
    /// Tests SNS Attach
    ///
    /// Data flow:
    /// Test.Client -> Server.OnSuccess -> Settings.SuccessTopicArn -> CoreWCF.Success-q
    ///
    /// This requires manual infrastructure setup:
    /// - Create a SNS Topic (and save arn to Settings file)
    /// - Create a new Queue (CoreWCF.Success-q)
    /// - Create a SNS Topic Subscription so that CoreWCF.Success-q is notified.
    /// </summary>
    [Fact]
    public async Task ServerEmitsDefaultSnsEvent()
    {
        var coreWcfQueueName = nameof(ServerEmitsDefaultSnsEvent) + Guid.NewGuid();
        var logMessage = coreWcfQueueName + "-LogMessage";

        // Fixture will automatically setup SNS callbacks
        // as long as the appsettings.test.json is populated with valid
        // sns topic arns
        _clientAndServerFixture.Start(_output, coreWcfQueueName);

        var clientService = _clientAndServerFixture.Channel!;

        var successQueueUrl =
            (await _clientAndServerFixture.SqsClient!.GetQueueUrlAsync(_clientAndServerFixture.Settings.AWS.SUCCESS_QUEUE_NAME))
            .QueueUrl;

        Assert.NotEmpty(successQueueUrl);
    
        // ACT
        clientService.LogMessage(logMessage);

        var receivedSuccessNotification = false;
        // poll for success messages (up to 20 seconds)
        for (var polling = 0; polling < 40; polling++)
        {
            var messages = await _clientAndServerFixture.SqsClient.ReceiveMessageAsync(successQueueUrl);

            // sns message body contains a message of the corewcf queue that originally 
            // received the message
            if (messages.Messages.Any(m => m.Body.Contains(coreWcfQueueName)))
            {
                receivedSuccessNotification = true;
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        // ASSERT
        try
        {
            Assert.True(receivedSuccessNotification);
        }
        finally
        {
            var queueUrlResponse = await _clientAndServerFixture.SqsClient.GetQueueUrlAsync(coreWcfQueueName);
            await _clientAndServerFixture.SqsClient.DeleteQueueAsync(queueUrlResponse.QueueUrl);
        }
    }

    public void Dispose()
    {
        _clientAndServerFixture.Dispose();
    }
}