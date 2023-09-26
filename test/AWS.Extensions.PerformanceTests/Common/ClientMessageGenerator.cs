using System.Net;
using System.ServiceModel;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
using NSubstitute;

namespace AWS.Extensions.PerformanceTests.Common;

public static class ClientMessageGenerator
{
    public static async Task SaturateQueue(
        IAmazonSQS setupSqsClient,
        string queueName,
        string queueUrl,
        int numMessages = 1000
    )
    {
        var message = $"{queueName}-Message";

        var rawMessage = BuildRawClientMessage<ILoggingService>(
            queueUrl,
            loggingClient => loggingClient.LogMessage(message)
        );

        for (var j = 0; j < numMessages / 10; j++)
        {
            var batchMessages = Enumerable
                .Range(0, 10)
                .Select(_ => new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), rawMessage))
                .ToList();

            await setupSqsClient!.SendMessageBatchAsync(queueUrl, batchMessages);
        }

        Console.WriteLine("Queue Saturation Complete");
    }

    public static string BuildRawClientMessage<TContract>(string queueUrl, Action<TContract> clientAction)
        where TContract : class
    {
        var fakeQueueName = "fake";
        var mockSqs = Substitute.For<IAmazonSQS>();

        // intercept the call the client will make to SendMessageAsync and capture the SendMessageRequest
        SendMessageRequest? capturedSendMessageRequest = null;

        mockSqs
            .SendMessageAsync(
                Arg.Do<SendMessageRequest>(r =>
                {
                    capturedSendMessageRequest = r;
                }),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(new SendMessageResponse { HttpStatusCode = HttpStatusCode.OK }));

        mockSqs
            .GetQueueUrlAsync(Arg.Any<string>())
            .Returns(Task.FromResult(new GetQueueUrlResponse { QueueUrl = queueUrl }));

        var sqsBinding = new WCF.Extensions.SQS.AwsSqsBinding(mockSqs, fakeQueueName);
        var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
        var factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
        var channel = factory.CreateChannel();
        ((System.ServiceModel.Channels.IChannel)channel).Open();

        var client = (TContract)channel;

        clientAction.Invoke(client);

        return capturedSendMessageRequest?.MessageBody ?? "";
    }
}
