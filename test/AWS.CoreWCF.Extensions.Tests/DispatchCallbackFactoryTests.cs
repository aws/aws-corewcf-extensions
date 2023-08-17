using System.Diagnostics.CodeAnalysis;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using AWS.CoreWCF.Extensions.SQS.Channels;
using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
using CoreWCF;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests
{
    public class DispatchCallbackFactoryTests
    {
        const string FakeTopicArn = "fakeTopicArm";

        [Fact]
        public void SendsNotificationOnFailure()
        {
            // ARRANGE
            var fakeMessageContext = new AwsSqsMessageContext
            {
                LocalAddress = new EndpointAddress("http://fake"),
                MessageReceiptHandle = "fakeHandle"
            };

            var fakeSns = Substitute.For<IAmazonSimpleNotificationService>();

            var fakeServices = new ServiceCollection()
                .AddSingleton<IAmazonSimpleNotificationService>(fakeSns)
                .BuildServiceProvider();

            var failureNotification = DispatchCallbackFactory.GetDefaultFailureNotificationCallbackWithSns(
                FakeTopicArn
            );

            // ACT
            failureNotification.Invoke(fakeServices, fakeMessageContext);

            // ASSERT
            // assert fake publish request
            fakeSns
                .Received()
                .PublishAsync(
                    Arg.Is<PublishRequest>(
                        req =>
                            // make sure we are sending a failure notification
                            req.Message.Contains("Failed")
                            &&
                            // make sure we are sending a notification about the correct queue
                            req.Message.Contains(fakeMessageContext.MessageReceiptHandle)
                    ),
                    Arg.Any<CancellationToken>()
                );
        }

        [Fact]
        [ExcludeFromCodeCoverage]
        public void NotificationCallbackSuppressesExceptions()
        {
            // ARRANGE
            var fakeMessageContext = new AwsSqsMessageContext
            {
                LocalAddress = new EndpointAddress("http://fake"),
                MessageReceiptHandle = "fakeHandle"
            };

            var fakeSns = Substitute.For<IAmazonSimpleNotificationService>();
            fakeSns
                .PublishAsync(Arg.Any<PublishRequest>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new Exception("Fake Exception"));

            var fakeServices = new ServiceCollection()
                .AddSingleton<IAmazonSimpleNotificationService>(fakeSns)
                .BuildServiceProvider();

            var failureNotification = DispatchCallbackFactory.GetDefaultFailureNotificationCallbackWithSns(
                FakeTopicArn
            );

            // ACT
            // capture any exceptions thrown by the Invoke method
            Exception? capturedException = null;
            try
            {
                failureNotification.Invoke(fakeServices, fakeMessageContext);
            }
            catch (Exception e)
            {
                capturedException = e;
            }

            // ASSERT
            // make sure we didn't see any exceptions bubble up from Invoke()
            Assert.Null(capturedException);
        }
    }
}
