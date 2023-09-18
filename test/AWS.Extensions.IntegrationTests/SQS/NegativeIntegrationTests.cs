using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.SecurityToken.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Extensions.IntegrationTests.SQS.TestHelpers;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS;

[Collection("ClientAndServer collection")]
[ExcludeFromCodeCoverage]
public class NegativeIntegrationTests : IDisposable
{
    public const string SqsReadOnlyRoleName = "IntegrationTestsSqsReadOnlyRole";

    private readonly ITestOutputHelper _output;
    private readonly ClientAndServerFixture _clientAndServerFixture;

    public NegativeIntegrationTests(ITestOutputHelper output, ClientAndServerFixture clientAndServerFixture)
    {
        _output = output;
        _clientAndServerFixture = clientAndServerFixture;
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public async Task ClientWithInsufficientQueuePermissionsThrowsException()
    {
        // ARRANGE
        string queueName = ClientAndServerFixture.QueueWithDefaultSettings;

        _clientAndServerFixture.Start(_output, queueName: queueName);

        var roleArn = await FindSqsReadOnlyRoleArn(_clientAndServerFixture.IamClient!);

        var assumedSqsReadOnlyCreds = await _clientAndServerFixture.StsClient!.AssumeRoleAsync(
            new AssumeRoleRequest
            {
                RoleArn = roleArn,
                RoleSessionName = nameof(ClientWithInsufficientQueuePermissionsThrowsException)
            }
        );

        // Create SQS Client with read-only perms
        var limitedSqsClient = new AmazonSQSClient(assumedSqsReadOnlyCreds.Credentials);

        // Start WCF Client with read-only perms
        var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(limitedSqsClient, queueName);
        var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
        var factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
        var channel = factory.CreateChannel();
        ((System.ServiceModel.Channels.IChannel)channel).Open();

        Exception? expectedException = null;

        // ACT
        try
        {
            channel.LogMessage("Expect this to fail - client doesn't have write permissions");
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.ShouldBeOfType<AggregateException>();
        expectedException.InnerException.ShouldNotBeNull();
        expectedException.InnerException.ShouldBeOfType<AmazonSQSException>();
        expectedException.InnerException.Message.ShouldContain(
            "no identity-based policy allows the sqs:sendmessage action"
        );
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public async Task ServerIgnoresMalformedMessage()
    {
        // ARRANGE
        string queueName = nameof(ServerIgnoresMalformedMessage) + DateTime.Now.Ticks;

        _clientAndServerFixture.Start(_output, queueName, new CreateQueueRequest(queueName));

        var sqsClient = _clientAndServerFixture.SqsClient!;

        var queueUrl = (await sqsClient.GetQueueUrlAsync(queueName)).QueueUrl;

        // ACT

        // send 5 good messages, then 1 bad message, then 5 good messages
        for (var i = 0; i < 5; i++)
            _clientAndServerFixture.Channel!.LogMessage($"{nameof(ServerIgnoresMalformedMessage)}-1. " + i);

        // send a bad message
        await sqsClient.SendMessageAsync(queueUrl, $"{nameof(ServerIgnoresMalformedMessage)} - Not a Soap Message");

        for (var i = 0; i < 5; i++)
            _clientAndServerFixture.Channel!.LogMessage($"{nameof(ServerIgnoresMalformedMessage)}-2. " + i);

        var serverReceivedAllMessages = false;

        // poll for up to 20 seconds
        for (var polling = 0; polling < 40; polling++)
        {
            if (10 == LoggingService.LogResults.Count(m => m.Contains(nameof(ServerIgnoresMalformedMessage))))
            {
                serverReceivedAllMessages = true;
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        _output.WriteLine($"Received: {string.Join(",", LoggingService.LogResults.ToArray())}");

        // ASSERT
        try
        {
            Assert.True(serverReceivedAllMessages);
        }
        finally
        {
            // cleanup
            await sqsClient.DeleteQueueAsync(queueUrl);
        }
    }

    private async Task<string> FindSqsReadOnlyRoleArn(IAmazonIdentityManagementService iamClient)
    {
        await foreach (var role in iamClient.Paginators.ListRoles(new ListRolesRequest()).Roles)
        {
            if (role.RoleName.StartsWith(SqsReadOnlyRoleName))
                return role.Arn;
        }

        throw new Exception("Failed to find Role needed for Testing.  Was CDK Run?");
    }

    public void Dispose()
    {
        _clientAndServerFixture.Dispose();
    }
}
