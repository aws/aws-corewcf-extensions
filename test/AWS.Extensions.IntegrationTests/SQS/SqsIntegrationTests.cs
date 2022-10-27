using AWS.Extensions.IntegrationTests.Common;
using AWS.Extensions.IntegrationTests.SQS.TestHelpers;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

[Collection("ClientAndServer collection")]
public class SqsIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private static ClientAndServerFixture _clientAndServerFixture;

    public SqsIntegrationTests(ITestOutputHelper output, ClientAndServerFixture clientAndServerFixture)
    {
        _output = output;
        _clientAndServerFixture = clientAndServerFixture;
    }

    [Fact]
    public async Task Server_Reads_And_Dispatches_Message_From_Sqs()
    {
        var sqsClient = _clientAndServerFixture.SqsClient;
        var queueUrl = _clientAndServerFixture.GetQueueName();
        var credentials = ClientAndServerFixture.GetCredentials();

        var testCaseName = nameof(Server_Reads_And_Dispatches_Message_From_Sqs);
        LoggingService.InitializeTestCase(testCaseName);

        await MessageHelper.SendMessageToQueueAsync(
            nameof(ILoggingService), 
            nameof(ILoggingService.LogMessage),
            testCaseName,
            queueUrl,
            credentials);
            
        Assert.True(LoggingService.LogResults[testCaseName].Wait(TimeSpan.FromSeconds(5)));
        await SqsAssert.QueueIsEmpty(sqsClient, queueUrl);
    }
}