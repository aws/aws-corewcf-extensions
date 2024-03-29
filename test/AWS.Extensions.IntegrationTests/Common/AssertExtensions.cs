﻿using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using AWS.CoreWCF.Extensions.Common;
using Xunit;

namespace AWS.Extensions.IntegrationTests.Common;

[ExcludeFromCodeCoverage]
public static class SqsAssert
{
    public static async Task QueueIsEmpty(
        IAmazonSQS sqsClient,
        string queueName,
        int maxRetries = 3,
        int retryDelayInSeconds = 1
    )
    {
        var queueUrlResponse = await sqsClient.GetQueueUrlAsync(queueName);
        var queueUrl = queueUrlResponse.QueueUrl;

        var queueIsEmpty = false;
        var attributesList = new List<string> { "All" };

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            var response = await sqsClient.GetQueueAttributesAsync(queueUrl, attributesList);
            response.Validate();

            var messageCounts = new List<int>
            {
                response.ApproximateNumberOfMessages,
                response.ApproximateNumberOfMessagesNotVisible,
                response.ApproximateNumberOfMessagesDelayed
            };
            queueIsEmpty = messageCounts.All(messageCount => messageCount == 0);
            if (queueIsEmpty)
            {
                break;
            }

            await Task.Delay(retryDelayInSeconds * 1000);
        }
        Assert.True(queueIsEmpty);
    }

    public static async Task ClearQueues(this IAmazonSQS sqsClient, params string[] queueNames)
    {
        foreach (var queue in queueNames)
        {
            var queueUrl = (await sqsClient.GetQueueUrlAsync(queue)).QueueUrl;
            await sqsClient.PurgeQueueAsync(queueUrl);
        }
    }
}
