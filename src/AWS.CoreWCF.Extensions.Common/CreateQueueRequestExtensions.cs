﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS.Model;
using Amazon.SQS;

namespace AWS.CoreWCF.Extensions.Common;

public static class CreateQueueRequestExtensions
{
    private const string DefaultSQSTag = "CoreWCFExtensionsSQS";

    public static CreateQueueRequest SetDefaultValues(this CreateQueueRequest request, string? queueName = null)
    {
        request.QueueName = queueName ?? request.QueueName;
        request.Attributes = GetDefaultAttributeValues();
        request.Tags = new Dictionary<string, string> { { DefaultSQSTag, DefaultSQSTag } };
        return request;
    }

    public static CreateQueueRequest WithFIFO(this CreateQueueRequest request, bool useFIFO = true)
    {
        request.Attributes[QueueAttributeName.FifoQueue] = useFIFO.ToString();
        if (useFIFO)
        {
            request.Attributes[QueueAttributeName.ContentBasedDeduplication] = true.ToString();
        }

        return request;
    }

    public static CreateQueueRequest WithDeadLetterQueue(
        this CreateQueueRequest request,
        int maxReceiveCount = 1,
        string? deadLetterTargetArn = null
    )
    {
        var redrivePolicy = new Dictionary<string, string> { { nameof(maxReceiveCount), maxReceiveCount.ToString() } };

        if (!string.IsNullOrEmpty(deadLetterTargetArn))
        {
            redrivePolicy[nameof(deadLetterTargetArn)] = deadLetterTargetArn;
        }

        request.Attributes[QueueAttributeName.RedrivePolicy] = JsonSerializer.Serialize(redrivePolicy);
        return request;
    }

    public static CreateQueueRequest WithManagedServerSideEncryption(
        this CreateQueueRequest request,
        bool useManagedServerSideEncryption = true
    )
    {
        if (useManagedServerSideEncryption)
        {
            request.Attributes.Remove(QueueAttributeName.KmsMasterKeyId);
            request.Attributes.Remove(QueueAttributeName.KmsDataKeyReusePeriodSeconds);
        }

        request.Attributes[QueueAttributeName.SqsManagedSseEnabled] = useManagedServerSideEncryption.ToString();
        return request;
    }

    public static CreateQueueRequest WithKMSEncryption(
        this CreateQueueRequest request,
        string kmsMasterKeyId,
        int kmsDataKeyReusePeriodInSeconds = 300
    )
    {
        request.Attributes[QueueAttributeName.SqsManagedSseEnabled] = false.ToString();
        request.Attributes[QueueAttributeName.KmsMasterKeyId] = kmsMasterKeyId;
        request.Attributes[QueueAttributeName.KmsDataKeyReusePeriodSeconds] = kmsDataKeyReusePeriodInSeconds.ToString();

        return request;
    }

    public static CreateQueueRequest WithoutKMSEncryption(this CreateQueueRequest request)
    {
        request.Attributes.Remove(QueueAttributeName.KmsMasterKeyId);
        request.Attributes.Remove(QueueAttributeName.KmsDataKeyReusePeriodSeconds);

        return request;
    }

    private const int MaxSQSMessageSizeInBytes = 262144; // 2^18
    private const int MaxSQSMessageRetentionPeriodInSeconds = 345600; // 4 days
    private const int DefaultDelayInSeconds = 0;
    private const int DefaultReceiveMessageWaitTimeSeconds = 0;
    private const int DefaultVisibilityTimeoutInSeconds = 30;
    private const int DefaultKmsDataKeyReusePeriodInSeconds = 300; // 5 minutes

    private static Dictionary<string, string> GetDefaultAttributeValues()
    {
        var defaultAttributes = new Dictionary<string, string>
        {
            { QueueAttributeName.DelaySeconds, DefaultDelayInSeconds.ToString() },
            { QueueAttributeName.MaximumMessageSize, MaxSQSMessageSizeInBytes.ToString() },
            { QueueAttributeName.MessageRetentionPeriod, MaxSQSMessageRetentionPeriodInSeconds.ToString() },
            { QueueAttributeName.ReceiveMessageWaitTimeSeconds, DefaultReceiveMessageWaitTimeSeconds.ToString() },
            { QueueAttributeName.VisibilityTimeout, DefaultVisibilityTimeoutInSeconds.ToString() },
            { QueueAttributeName.KmsDataKeyReusePeriodSeconds, DefaultKmsDataKeyReusePeriodInSeconds.ToString() },
            { QueueAttributeName.SqsManagedSseEnabled, false.ToString() }
        };
        return defaultAttributes;
    }

    public static bool IsFIFO(this CreateQueueRequest createQueueRequest)
    {
        return createQueueRequest.Attributes.TryGetValue(QueueAttributeName.FifoQueue, out var isFifoString)
            && isFifoString.Equals(true.ToString(), StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool IsUsingDeadLetterQueue(this CreateQueueRequest createQueueRequest)
    {
        return createQueueRequest.GetRedrivePolicy() is not null;
    }

    public static Dictionary<string, string>? GetRedrivePolicy(this CreateQueueRequest createQueueRequest)
    {
        if (
            createQueueRequest.Attributes.TryGetValue(QueueAttributeName.RedrivePolicy, out var redrivePolicyString)
            && JsonSerializer.Deserialize<Dictionary<string, string>>(redrivePolicyString)
                is Dictionary<string, string> redrivePolicy
        )
        {
            return redrivePolicy;
        }
        return null;
    }

    public static bool IsUsingManagedServerSideEncryption(this CreateQueueRequest createQueueRequest)
    {
        return createQueueRequest.Attributes.TryGetValue(
                QueueAttributeName.SqsManagedSseEnabled,
                out var managedSseEnabled
            ) && managedSseEnabled.Equals(true.ToString(), StringComparison.InvariantCultureIgnoreCase);
    }

    public static bool IsUsingKMS(this CreateQueueRequest createQueueRequest)
    {
        return createQueueRequest.GetRedrivePolicy() is not null;
    }
}
