using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using Shouldly;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests;

public class AqsSqsBindingTests
{
    [Fact]
    public void PropertiesComeFromTransport()
    {
        // ARRANGE
        var fakeDispatch = new DispatchCallbacksCollection();
        const long fakeMessageSize = 42L;
        const string fakeQueue = "fakeQueue";

        var awsSqsBinding = new AwsSqsBinding { MaxMessageSize = 1 };

        // ACT
        var transport = awsSqsBinding.CreateBindingElements().OfType<AwsSqsTransportBindingElement>().First();

        transport.MaxReceivedMessageSize = fakeMessageSize;
        transport.DispatchCallbacksCollection = fakeDispatch;
        transport.QueueName = fakeQueue;

        // ASSERT
        awsSqsBinding.DispatchCallbacksCollection.ShouldBe(fakeDispatch);
        awsSqsBinding.MaxMessageSize.ShouldBe(fakeMessageSize);
        awsSqsBinding.QueueName.ShouldBe(fakeQueue);
        awsSqsBinding.Scheme.ShouldNotBeNullOrEmpty();
    }
}
