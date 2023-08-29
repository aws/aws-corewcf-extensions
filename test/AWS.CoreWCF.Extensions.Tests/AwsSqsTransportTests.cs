using System.Diagnostics.CodeAnalysis;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.Infrastructure;
using CoreWCF.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests;

/// <summary>
/// Negative tests for <see cref="AwsSqsTransport"/>
/// </summary>
public class AwsSqsTransportTests
{
    [Fact]
    [ExcludeFromCodeCoverage]
    public async Task ReceiveQueueMessageContextRethrowsExceptions()
    {
        // ARRANGE
        var fakeException = new Exception("fake");

        var mockSqsMessageProvider = Substitute.For<ISQSMessageProvider>();

        mockSqsMessageProvider.ReceiveMessageAsync(Arg.Any<string>()).ThrowsAsync(fakeException);

        var fakeServices = new ServiceCollection().AddSingleton(mockSqsMessageProvider).BuildServiceProvider();

        var awsSqsTransport = new AwsSqsTransport(
            fakeServices,
            Substitute.For<IServiceDispatcher>(),
            null,
            null,
            null,
            new Logger<AwsSqsTransport>(Substitute.For<ILoggerFactory>())
        );

        Exception? expectedException = null;

        // ACT
        try
        {
            await awsSqsTransport.ReceiveQueueMessageContextAsync(CancellationToken.None);
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.ShouldBe(fakeException);
    }
}
