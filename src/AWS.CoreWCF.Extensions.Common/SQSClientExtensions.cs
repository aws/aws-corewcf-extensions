using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace AWS.CoreWCF.Extensions.Common;

public static class SQSClientExtensions
{
    /// <summary>
    /// Uses the response object to determine if the http request was successful.
    /// </summary>
    /// <param name="response">Response to validate with</param>
    /// <exception cref="HttpRequestException">Thrown if http request was unsuccessful</exception>
    public static void Validate(this AmazonWebServiceResponse response)
    {
        var statusCode = (int)response.HttpStatusCode;
        if (statusCode < 200 || statusCode >= 300)
            throw new HttpRequestException($"HttpStatusCode: {statusCode}");
    }

    public static async Task<IEnumerable<Amazon.SQS.Model.Message>> ReceiveMessagesAsync(
        this IAmazonSQS sqsClient,
        string queueUrl,
        ILogger logger,
        int maxMessagesToReceive = 10
    )
    {
        var request = new ReceiveMessageRequest { QueueUrl = queueUrl, MaxNumberOfMessages = maxMessagesToReceive };
        try
        {
            var receiveMessageResponse = await sqsClient.ReceiveMessageAsync(request);
            receiveMessageResponse.Validate();

            return receiveMessageResponse.Messages;
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                $"Error while attempting to retrieve messages from {queueUrl}. Returning empty collection."
            );
            return new List<Message>();
        }
    }

    public static async Task DeleteMessageAsync(
        this IAmazonSQS sqsClient,
        string queueUrl,
        string receiptHandle,
        ILogger logger
    )
    {
        var deleteMessageRequest = new DeleteMessageRequest { QueueUrl = queueUrl, ReceiptHandle = receiptHandle };
        try
        {
            var deleteMessageResponse = await sqsClient.DeleteMessageAsync(deleteMessageRequest);
            deleteMessageResponse.Validate();
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                $"Error while attempting to delete message from {queueUrl}. Receipt handle: {receiptHandle}"
            );
        }
    }

    public static async Task<string> GetQueueArnAsync(this IAmazonSQS sqsClient, string queueUrl)
    {
        const string arnAttribute = "QueueArn";
        var attributesResponse = await sqsClient.GetQueueAttributesAsync(
            new GetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                AttributeNames = new List<string> { arnAttribute }
            }
        );
        attributesResponse.Validate();
        return attributesResponse.QueueARN;
    }

    public static async Task<string> EnsureSQSQueue(this IAmazonSQS sqsClient, CreateQueueRequest createQueueRequest) =>
        await sqsClient.EnsureSQSQueue(createQueueRequest.QueueName, () => createQueueRequest);

    public static async Task<string> EnsureSQSQueue(
        this IAmazonSQS sqsClient,
        string queueName,
        Func<CreateQueueRequest> createQueueRequestBuilder
    )
    {
        try
        {
            var response = await sqsClient.GetQueueUrlAsync(queueName);
            response.Validate();

            return response.QueueUrl;
        }
        catch (QueueDoesNotExistException)
        {
            try
            {
                var createQueueRequest = createQueueRequestBuilder();

                var createQueueRequestWithDlq = await sqsClient.EnsureDeadLetterQueue(createQueueRequest);
                var response = await sqsClient.CreateQueueAsync(createQueueRequestWithDlq);
                response.Validate();

                await sqsClient.WithBasicPolicy(queueName);

                return response.QueueUrl;
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to automatically create Queue [{queueName}]", e);
            }
        }
    }

    public static async Task<IAmazonSQS> WithBasicPolicy(this IAmazonSQS sqsClient, string queueName)
    {
        var queueUrl = (await sqsClient.GetQueueUrlAsync(queueName)).QueueUrl;
        var queueArn = await sqsClient.GetQueueArnAsync(queueUrl);
        var basicPolicy = BasicPolicyTemplates.GetBasicSQSPolicy(queueArn);

        return await sqsClient.WithPolicy(queueUrl, basicPolicy);
    }

    public static async Task<IAmazonSQS> WithPolicy(this IAmazonSQS sqsClient, string queueUrl, string policy)
    {
        var request = new SetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            Attributes = new Dictionary<string, string> { { QueueAttributeName.Policy, policy } }
        };
        var response = await sqsClient.SetQueueAttributesAsync(request);
        response.Validate();

        return sqsClient;
    }

    private static async Task<CreateQueueRequest> EnsureDeadLetterQueue(
        this IAmazonSQS sqsClient,
        CreateQueueRequest createQueueRequest
    )
    {
        if (!createQueueRequest.IsUsingDeadLetterQueue())
        {
            // Not using dead letter queue so do nothing
            return createQueueRequest;
        }

        var redrivePolicy = createQueueRequest.GetRedrivePolicy();
        if (
            true == redrivePolicy?.TryGetValue("deadLetterTargetArn", out var deadLetterTargetArn)
            && !string.IsNullOrEmpty(deadLetterTargetArn)
        )
        {
            // Dead letter queue ARN was provided so do nothing
            return createQueueRequest;
        }

        // Create request to create DLQ
        var dlqAttributes = new Dictionary<string, string>(createQueueRequest.Attributes);
        dlqAttributes.Remove(QueueAttributeName.RedrivePolicy);
        var dlqName = GenerateDlqNameFromCreateQueue(createQueueRequest);

        var createDlqRequest = new CreateQueueRequest { QueueName = dlqName, Attributes = dlqAttributes };

        // Create DLQ
        var createDlqResponse = await sqsClient.CreateQueueAsync(createDlqRequest);
        createDlqResponse.Validate();

        // Get DLQ ARN
        var dlqArn = await sqsClient.GetQueueArnAsync(createDlqResponse.QueueUrl);

        // Add DLQ ARN to existing redrive policy and return
        redrivePolicy["deadLetterTargetArn"] = dlqArn;
        createQueueRequest.Attributes[QueueAttributeName.RedrivePolicy] = JsonSerializer.Serialize(redrivePolicy);
        return createQueueRequest;
    }

    private static string GenerateDlqNameFromCreateQueue(CreateQueueRequest createQueueRequest)
    {
        const int maxQueueNameLength = 80;
        var dlqName = createQueueRequest.QueueName;
        var suffix = "-DLQ";
        if (createQueueRequest.IsFIFO())
        {
            suffix = "-DLQ.fifo";
            dlqName = dlqName.Replace(".fifo", string.Empty);
        }

        dlqName = dlqName.Substring(0, Math.Min(maxQueueNameLength - suffix.Length, dlqName.Length));
        return $"{dlqName}{suffix}";
    }
}
