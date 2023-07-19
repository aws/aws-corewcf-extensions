using System.Collections.Generic;
using Amazon.Runtime;
using Amazon.SQS;
using Newtonsoft.Json;

namespace AWS.CoreWCF.Server.SQS.Tests;

public class EnvironmentCollectionFixture
{
    private const string AwsKey = "AWS";
    private const string AccessKeyEnvVariable = "AWS_ACCESS_KEY_ID";
    private const string SecretKeyEnvVariable = "AWS_SECRET_ACCESS_KEY";
    private const string TestQueueUrlEnvVariable = "TEST_QUEUE_URL";
    private const string SuccessTopicArnEnvVariable = "SUCCESS_TOPIC_ARN";
    private const string FailureTopicArnEnvVariable = "FAILURE_TOPIC_ARN";

    public static string AccessKey { get; set; } = string.Empty;
    public static string SecretKey { get; set; } = string.Empty;
    public static string QueueUrl { get; set; } = string.Empty;
    public static string SuccessTopicArn { get; set; } = string.Empty;
    public static string FailureTopicArn { get; set; } = string.Empty;

    public static void InitializeEnvironment()
    {
        var json = File.ReadAllText("appsettings.test.json");
        var appSettingsDictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

        var settingsDict = appSettingsDictionary[AwsKey];
        AccessKey = settingsDict[AccessKeyEnvVariable];
        SecretKey = settingsDict[SecretKeyEnvVariable];
        QueueUrl = settingsDict[TestQueueUrlEnvVariable];
        SuccessTopicArn = settingsDict[SuccessTopicArnEnvVariable];
        FailureTopicArn = settingsDict[FailureTopicArnEnvVariable];
    }

    public static AWSCredentials GetCredentials()
    {
        return new BasicAWSCredentials(AccessKey, SecretKey);
    }

    public static IAmazonSQS GetSqsClient()
    {
        return new AmazonSQSClient(GetCredentials());
    }

    public static string? GetTestQueueUrl()
    {
        return QueueUrl;
    }
}
