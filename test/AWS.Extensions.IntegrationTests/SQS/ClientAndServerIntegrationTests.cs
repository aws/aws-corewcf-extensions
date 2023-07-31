using AWS.Extensions.IntegrationTests.Common;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

[Collection("ClientAndServer collection")]
public class ClientAndServerIntegrationTests
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
        var clientService = _clientAndServerFixture.Channel;
        var sqsClient = _clientAndServerFixture.SqsClient;
        var queueName = ClientAndServerFixture.QueueWithDefaultSettings;

        var testCaseName = nameof(Server_Reads_And_Dispatches_Message_From_Sqs);
        LoggingService.InitializeTestCase(testCaseName);
        clientService.LogMessage(testCaseName);

        Assert.True(LoggingService.LogResults[testCaseName].Wait(TimeSpan.FromSeconds(5)));
        await SqsAssert.QueueIsEmpty(sqsClient, queueName);
    }
}
