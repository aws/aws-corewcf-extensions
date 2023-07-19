using Amazon.Runtime;
using Amazon.SQS;

namespace AWS.Extensions.IntegrationTests.SQS.TestHelpers;

public class SdkClientHelper
{
    private static IAmazonSQS? _sqsClient;

    public static IAmazonSQS GetSqsClientInstance(AWSCredentials credentials)
    {
        _sqsClient ??= new AmazonSQSClient(credentials);
        return _sqsClient;
    }
}
