//using AWS.Extensions.IntegrationTests.Common;
//using AWS.Extensions.IntegrationTests.SQS.TestHelpers;
//using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
//using Xunit;
//using Xunit.Abstractions;

//namespace AWS.Extensions.IntegrationTests.SQS;

//[Collection("SQSEnvironment collection")]
//public class SqsIntegrationTests
//{
//    private readonly ITestOutputHelper _output;
//    private static SQSEnvironmentFixture s_sqsEnvironmentFixture;

//    public SqsIntegrationTests(ITestOutputHelper output, SQSEnvironmentFixture sqsEnvironmentFixture)
//    {
//        _output = output;
//        s_sqsEnvironmentFixture = sqsEnvironmentFixture;
//    }

//    [Fact]
//    public async Task Server_Reads_And_Dispatches_Message_From_Sqs()
//    {
//        var sqsClient = s_sqsEnvironmentFixture.SqsClient;
//        var queueUrl = s_sqsEnvironmentFixture.GetQueueName();
//        var credentials = SQSEnvironmentFixture.GetCredentials();

//        var testCaseName = nameof(Server_Reads_And_Dispatches_Message_From_Sqs);
//        LoggingService.InitializeTestCase(testCaseName);

//        await MessageHelper.SendMessageToQueueAsync(
//            nameof(ILoggingService),
//            nameof(ILoggingService.LogMessage),
//            testCaseName,
//            queueUrl,
//            credentials);

//        Assert.True(LoggingService.LogResults[testCaseName].Wait(TimeSpan.FromSeconds(5)));
//        await SqsAssert.QueueIsEmpty(sqsClient, queueUrl);
//    }
//}
