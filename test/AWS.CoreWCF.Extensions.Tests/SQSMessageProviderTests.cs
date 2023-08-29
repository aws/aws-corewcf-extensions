using System.Diagnostics.CodeAnalysis;
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
