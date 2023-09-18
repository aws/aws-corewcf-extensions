using System.Diagnostics.CodeAnalysis;
using System.Net;
using Amazon.Runtime;
using Amazon.Runtime.Internal.Transform;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests;

public class SQSClientExtensionTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Processing)]
    [ExcludeFromCodeCoverage]
    public void ValidateThrowsException(HttpStatusCode errorCode)
    {
        // ARRANGE
        var fakeAmazonWebServiceResponse = new AmazonWebServiceResponse { HttpStatusCode = errorCode };

        Exception? expectedException = null;

        // ACT
        try
        {
            fakeAmazonWebServiceResponse.Validate();
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task DeleteMessageSwallowsExceptions()
    {
        // ARRANGE
        var fakeSqsClient = Substitute.For<IAmazonSQS>();
        fakeSqsClient
            .DeleteMessageAsync(Arg.Any<DeleteMessageRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Testing"));

        // ACT
        await fakeSqsClient.DeleteMessageAsync("queueUrl", "recipeHandle", Substitute.For<ILogger>());

        // ASSERT
        // expect no exception to be thrown
    }

    [Fact]
    public async Task ReceiveMessagesSwallowsExceptions()
    {
        // ARRANGE
        var fakeSqsClient = Substitute.For<IAmazonSQS>();
        fakeSqsClient
            .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Testing"));

        // ACT
        await fakeSqsClient.ReceiveMessagesAsync("queueUrl", Substitute.For<ILogger>());

        // ASSERT
        // expect no exception to be thrown
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public async Task EnsureSQSQueueWrapsExceptions()
    {
        // ARRANGE
        const string fakeQueue = "fakeQueue";

        var fakeCreateQueueRequest = new CreateQueueRequest(fakeQueue);

        var fakeSqsClient = Substitute.For<IAmazonSQS>();
        fakeSqsClient
            .GetQueueUrlAsync(Arg.Is(fakeQueue), Arg.Any<CancellationToken>())
            .ThrowsAsync(new QueueDoesNotExistException(""));

        fakeSqsClient
            .CreateQueueAsync(Arg.Is(fakeQueue), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Testing"));

        Exception? expectedException = null;

        // ACT
        try
        {
            await fakeSqsClient.EnsureSQSQueue(fakeCreateQueueRequest);
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.Message.ShouldContain(fakeQueue);
        expectedException.Message.ShouldContain("Failed");
    }

    [Fact]
    public async Task EnsureQueueDoesNotTryToCreateDeadLetterQueueIfItAlreadyExists()
    {
        // ARRANGE
        const string fakeDlqArn = "dlq";

        const string fakeQueue = "fakeQueue";

        var fakeCreateQueueRequest = new CreateQueueRequest()
            .SetDefaultValues(fakeQueue)
            .WithDeadLetterQueue(deadLetterTargetArn: fakeDlqArn);

        var fakeSqsClient = Substitute.For<IAmazonSQS>();
        fakeSqsClient
            .GetQueueUrlAsync(Arg.Is(fakeQueue), Arg.Any<CancellationToken>())
            .Returns(
                // first pretend queue hasn't been created
                x => throw new QueueDoesNotExistException(""),
                // on second call, return a fake url
                x =>
                    Task.FromResult(
                        new GetQueueUrlResponse { HttpStatusCode = HttpStatusCode.OK, QueueUrl = "fakeUrl" }
                    )
            );

        fakeSqsClient
            .GetQueueAttributesAsync(Arg.Any<GetQueueAttributesRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new GetQueueAttributesResponse
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        Attributes = new Dictionary<string, string> { { "QueueArn", "fake:arn:with:parts" } }
                    }
                )
            );

        fakeSqsClient
            .SetQueueAttributesAsync(Arg.Any<SetQueueAttributesRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SetQueueAttributesResponse { HttpStatusCode = HttpStatusCode.OK }));

        // ok to create fakeQueue
        fakeSqsClient
            .CreateQueueAsync(Arg.Is<CreateQueueRequest>(req => req.QueueName == fakeQueue))
            .Returns(Task.FromResult(new CreateQueueResponse { HttpStatusCode = HttpStatusCode.OK }));

        // fail if attempting to create dlq
        fakeSqsClient
            .CreateQueueAsync(Arg.Is<CreateQueueRequest>(req => req.QueueName.Contains("DLQ")))
            .ThrowsAsync(new Exception("Fail: Should not try and create dlq"));

        // ACT
        await fakeSqsClient.EnsureSQSQueue(fakeCreateQueueRequest);

        // ASSERT
        // expect no exception to be thrown
    }
}
