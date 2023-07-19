using Amazon.SQS;

namespace AWS.CoreWCF.Server.SQS.Tests.TestHelpers;

public class SdkClientHelper
{
    private static IAmazonSQS? _sqsClient;

    public static IAmazonSQS GetSqsClientInstance()
    {
        _sqsClient ??= new AmazonSQSClient(EnvironmentCollectionFixture.GetCredentials());
        return _sqsClient;
    }
}
