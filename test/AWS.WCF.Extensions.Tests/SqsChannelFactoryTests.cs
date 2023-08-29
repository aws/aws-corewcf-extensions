using System.Diagnostics.CodeAnalysis;
using System.ServiceModel.Channels;
using AWS.WCF.Extensions.SQS;
using Shouldly;
using Xunit;

namespace AWS.WCF.Extensions.Tests;

/// <summary>
/// Negative tests for <see cref="SqsChannelFactory"/>
/// </summary>
public class SqsChannelFactoryTests
{
    [Fact]
    [ExcludeFromCodeCoverage]
    public void ThrowsExceptionIfTooManyEncodingElements()
    {
        // ARRANGE
        var element = new AWS.WCF.Extensions.SQS.AwsSqsTransportBindingElement(null, null);

        var badBindingContext = new BindingContext(
            new CustomBinding("binding", "ns"),
            new BindingParameterCollection
            {
                new TextMessageEncodingBindingElement(),
                new BinaryMessageEncodingBindingElement()
            }
        );

        Exception? expectedException = null;

        // ACT
        try
        {
            element.BuildChannelFactory<string>(badBindingContext);
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.ShouldBeOfType<InvalidOperationException>();
        expectedException.Message.ShouldContain("More than one");
    }

    [Fact]
    public void GetPropertyDefersToMessageEncoderFactoryForMessageVersion()
    {
        // ARRANGE
        var element = new AWS.WCF.Extensions.SQS.AwsSqsTransportBindingElement(null, null);

        var sqsChannelFactory = element.BuildChannelFactory<IOutputChannel>(
            new BindingContext(new CustomBinding("binding", "ns"), new BindingParameterCollection())
        );

        // ACT
        var messageVersion = sqsChannelFactory.GetProperty<MessageVersion>();

        // ASSERT
        messageVersion.ShouldNotBeNull();
        messageVersion.Addressing?.ToString().ShouldNotBeNullOrEmpty();
    }
}
