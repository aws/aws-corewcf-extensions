using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests;

public class ApplicationBuilderExtensionsTests
{
    [Fact]
    [ExcludeFromCodeCoverage]
    public void EnsureSqsQueueThrowsExceptionIfQueueIsNotInServiceProvider()
    {
        // ARRANGE
        const string fakeQueue = "fakeQueue";

        var namedSqsClientCollection = new NamedSQSClientCollection(
            new NamedSQSClient { SQSClient = Substitute.For<IAmazonSQS>(), QueueName = "Not" + fakeQueue }
        );

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
            .AddSingleton(namedSqsClientCollection)
            .BuildServiceProvider();

        var fakeApplicationBuilder = Substitute.For<IApplicationBuilder>();
        fakeApplicationBuilder.ApplicationServices.Returns(services);

        Exception? expectedException = null;

        // ACT
        try
        {
            fakeApplicationBuilder.EnsureSqsQueue(fakeQueue, new CreateQueueRequest(fakeQueue));
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
}
