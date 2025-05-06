using System.Diagnostics.CodeAnalysis;
using System.Net;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests;

public class SQSMessageProviderTests
{
    public static IEnumerable<object[]> ConstructorThrowsExceptionIfNamedSQSClientIsNotFullyFormedData()
    {
        yield return new object[] { Substitute.For<IAmazonSQS>(), null };
        yield return new object[] { null, "queue" };
    }

    [Theory]
    [MemberData(nameof(ConstructorThrowsExceptionIfNamedSQSClientIsNotFullyFormedData))]
    [ExcludeFromCodeCoverage]
    public void ConstructorThrowsExceptionIfNamedSQSClientIsNotFullyFormed(IAmazonSQS sqs, string queue)
    {
        // ARRANGE
        Exception? expectedException = null;

        var invalidNamedSqsClientCollection = new NamedSQSClientCollection(
            new NamedSQSClient { SQSClient = sqs, QueueName = queue }
        );

        // ACT
        try
        {
            new SQSMessageProvider(
                new[] { invalidNamedSqsClientCollection },
                new Logger<SQSMessageProvider>(Substitute.For<ILoggerFactory>())
            );
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.Message.ShouldContain("Invalid");
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public void ConstructorThrowsExceptionIfQueueIsNotProvisioned()
    {
        // ARRANGE
        const string fakeQueue = "fakeQueue";

        var fakeSqsClient = Substitute.For<IAmazonSQS>();
        fakeSqsClient
            .GetQueueUrlAsync(Arg.Is(fakeQueue), Arg.Any<CancellationToken>())
            .ThrowsAsync(new QueueDoesNotExistException(""));

        var invalidNamedSqsClientCollection = new NamedSQSClientCollection(
            new NamedSQSClient { SQSClient = fakeSqsClient, QueueName = fakeQueue }
        );

        Exception? expectedException = null;

        // ACT
        try
        {
            new SQSMessageProvider(
                new[] { invalidNamedSqsClientCollection },
                new Logger<SQSMessageProvider>(Substitute.For<ILoggerFactory>())
            );
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.ShouldBeOfType<ArgumentException>();
        expectedException.Message.ShouldContain(fakeQueue);
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public void ReceiveMessageIgnoresDuplicateNamedSqsClients()
    {
        // ARRANGE
        var fakeSqsClient = Substitute.For<IAmazonSQS>();
        fakeSqsClient
            .GetQueueUrlAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetQueueUrlResponse()));
        fakeSqsClient
            .GetQueueAttributesAsync(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetQueueAttributesResponse { Attributes = new() }));

        var listWithDuplicate = new List<NamedSQSClientCollection>
        {
            new NamedSQSClientCollection(
                new NamedSQSClient
                {
                    QueueName = nameof(ReceiveMessageIgnoresDuplicateNamedSqsClients),
                    SQSClient = fakeSqsClient
                },
                new NamedSQSClient
                {
                    QueueName = nameof(ReceiveMessageIgnoresDuplicateNamedSqsClients),
                    SQSClient = fakeSqsClient
                }
            )
        };

        // ACT
        var sqsMessageProvider = new SQSMessageProvider(
            listWithDuplicate,
            new Logger<SQSMessageProvider>(Substitute.For<ILoggerFactory>())
        );

        // ASSERT
        // expect no exception to be thrown
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public async Task ReceiveMessageThrowsExceptionIfQueueNameNotFound()
    {
        // ARRANGE
        const string fakeQueue = "fakeQueue";

        var sqsMessageProvider = new SQSMessageProvider(
            new List<NamedSQSClientCollection>(),
            new Logger<SQSMessageProvider>(Substitute.For<ILoggerFactory>())
        );

        Exception? expectedException = null;

        // ACT
        try
        {
            await sqsMessageProvider.ReceiveMessageAsync(fakeQueue);
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.Message.ShouldContain(fakeQueue);
    }

    [Fact]
    public async Task ReceiveMessageHonorsVisibilityTimeout()
    {
        // ARRANGE
        const string fakeQueueName = nameof(ReceiveMessageHonorsVisibilityTimeout);
        const string batch1Message = "Batch 1";
        const string batch2Message = "Batch 2";

        var fakeSqsClient = Substitute.For<IAmazonSQS>();
        fakeSqsClient
            .GetQueueUrlAsync(Arg.Is(fakeQueueName), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetQueueUrlResponse()));
        fakeSqsClient
            .GetQueueAttributesAsync(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(
                    new GetQueueAttributesResponse
                    {
                        Attributes = new Dictionary<string, string>
                        {
                            { QueueAttributeName.VisibilityTimeout, "1" } // 1 second is the lowest value to keep the Timeout "on"
                        }
                    }
                )
            );

        fakeSqsClient
            .ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                // first call to ReceiveMessageAsync returns 5 'Batch 1' messages
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        Messages = Enumerable.Range(0, 5).Select(_ => new Message { Body = batch1Message }).ToList()
                    }
                ),
                // second call to ReceiveMessageAsync returns 5 'Batch 2' messages
                Task.FromResult(
                    new ReceiveMessageResponse
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        Messages = Enumerable.Range(0, 5).Select(_ => new Message { Body = batch2Message }).ToList()
                    }
                )
            );

        var namedClients = new List<NamedSQSClientCollection>
        {
            new(new NamedSQSClient { QueueName = fakeQueueName, SQSClient = fakeSqsClient })
        };

        var sqsMessageProvider = new SQSMessageProvider(
            namedClients,
            new Logger<SQSMessageProvider>(Substitute.For<ILoggerFactory>())
        );

        // ACT

        // get 1 message from provider - this should return a message from batch 1
        var firstMessage = await sqsMessageProvider.ReceiveMessageAsync(fakeQueueName);

        // wait slightly longer than the message visibility timeout so that the cache expires
        await Task.Delay(TimeSpan.FromSeconds(1.2));

        // this should require a new call to SqsClient.ReceiveMessage, which should
        // return a message from batch 2
        var secondMessage = await sqsMessageProvider.ReceiveMessageAsync(fakeQueueName);

        // ASSERT
        firstMessage!.Body.ShouldBe(batch1Message);

        secondMessage!.Body.ShouldBe(batch2Message);
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public async Task DeleteMessageThrowsExceptionIfQueueNameNotFound()
    {
        // ARRANGE
        const string fakeQueue = "fakeQueue";

        var sqsMessageProvider = new SQSMessageProvider(
            new List<NamedSQSClientCollection>(),
            new Logger<SQSMessageProvider>(Substitute.For<ILoggerFactory>())
        );

        Exception? expectedException = null;

        // ACT
        try
        {
            await sqsMessageProvider.DeleteSqsMessageAsync(fakeQueue, "receiptHandle");
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.Message.ShouldContain(fakeQueue);
    }
}
