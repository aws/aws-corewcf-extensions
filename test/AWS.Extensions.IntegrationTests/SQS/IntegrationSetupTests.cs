using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

public class IntegrationSetupTests
{
    private readonly ITestOutputHelper _testOutput;

    public IntegrationSetupTests(ITestOutputHelper testOutput)
    {
        _testOutput = testOutput;
    }

    [Fact]
    public async Task CanAccessQueues()
    {
        var sqsClient = new AmazonSQSClient();

        var response = await sqsClient.ListQueuesAsync(new ListQueuesRequest { MaxResults = 20 });

        response.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
        response.QueueUrls.Count.ShouldBeGreaterThan(1);

        foreach (var result in response.QueueUrls)
        {
            _testOutput.WriteLine(result);
            Console.WriteLine(result);
        }
    }
}
