using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Amazon.SQS;
using AWS.WCF.Extensions.SQS;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AWS.WCF.Extensions.Tests;

/// <summary>
/// Negative tests for <see cref="SqsOutputChannel"/>
/// </summary>
public class SqsOutputChannelTests
{
    private readonly SqsChannelFactory _sqsChannelFactory;

    public SqsOutputChannelTests()
    {
        _sqsChannelFactory =
            new AwsSqsTransportBindingElement(null, null).BuildChannelFactory<IOutputChannel>(
                new BindingContext(new CustomBinding("binding", "ns"), new BindingParameterCollection())
            ) as SqsChannelFactory;
    }

    [Fact]
    [ExcludeFromCodeCoverage]
    public void ConstructorThrowsArgumentException()
    {
        // ARRANGE
        var badVia = new Uri("http://bad");
        Exception? expectedException = null;

        // ACT
        try
        {
            new SqsOutputChannel(
                _sqsChannelFactory,
                Substitute.For<IAmazonSQS>(),
                new EndpointAddress("http://fake"),
                badVia,
                null
            );
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.ShouldBeOfType<ArgumentException>();
        expectedException.Message.ShouldContain("scheme");
    }

    [Fact]
    public void ViaPropertyIsSet()
    {
        // ARRANGE
        IOutputChannel outputChannel = new SqsOutputChannel(
            _sqsChannelFactory,
            Substitute.For<IAmazonSQS>(),
            new EndpointAddress("http://fake"),
            via: new Uri("https://fake"),
            null
        );

        // ACT
        var via = outputChannel.Via;

        // ASSERT
        via.ShouldNotBeNull();
    }

    [Fact]
    public void GetPropertyReturnsSqsOutputChannel()
    {
        // ARRANGE
        IOutputChannel outputChannel = new SqsOutputChannel(
            _sqsChannelFactory,
            Substitute.For<IAmazonSQS>(),
            new EndpointAddress("http://fake"),
            via: new Uri("https://fake"),
            null
        );

        // ACT
        var property = outputChannel.GetProperty<IOutputChannel>();

        // ASSERT
        property.ShouldBe(outputChannel);
    }

    [Fact]
    public void GetPropertyFallsBackToEncoder()
    {
        // ARRANGE
        var mockEncoder = Substitute.For<MessageEncoder>();

        IOutputChannel outputChannel = new SqsOutputChannel(
            _sqsChannelFactory,
            NSubstitute.Substitute.For<IAmazonSQS>(),
            new EndpointAddress("http://fake"),
            via: new Uri("https://fake"),
            mockEncoder
        );

        // ACT
        outputChannel.GetProperty<string>();

        // ASSERT
        mockEncoder.Received().GetProperty<string>();
    }
}
