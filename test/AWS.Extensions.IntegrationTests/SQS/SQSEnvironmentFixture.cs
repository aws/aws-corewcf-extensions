//using System.ServiceModel;
//using Amazon.Extensions.NETCore.Setup;
//using Amazon.Runtime;
//using Amazon.SimpleNotificationService;
//using Amazon.SQS;
//using Amazon.SQS.Model;
//using AWS.CoreWCF.Extensions.Common;
//using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;
//using AWS.CoreWCF.Extensions.SQS.Infrastructure;
//using AWS.Extensions.IntegrationTests.SQS.TestService;
//using AWS.Extensions.IntegrationTests.SQS.TestService.ServiceContract;
//using CoreWCF.Configuration;
//using CoreWCF.Queue.Common.Configuration;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Newtonsoft.Json;
//using Xunit;

//namespace AWS.Extensions.IntegrationTests.SQS;

//[CollectionDefinition("SQSEnvironment collection")]
//public class SQSEnvironmentCollectionFixture : ICollectionFixture<SQSEnvironmentFixture>
//{
//    // This class has no code, and is never created. Its purpose is simply
//    // to be the place to apply [CollectionDefinition] and all the
//    // ICollectionFixture<> interfaces.
//}

//public class SQSEnvironmentFixture //: IDisposable
//{
//    private const string AwsKey = "AWS";
//    private const string ProfileEnvVariable = "PROFILE";
//    //private const string AccessKeyEnvVariable = "AWS_ACCESS_KEY_ID";
//    //private const string SecretKeyEnvVariable = "AWS_SECRET_ACCESS_KEY";
//    private const string TestQueueNameEnvVariable = "TEST_QUEUE_NAME";
//    private const string SuccessTopicArnEnvVariable = "SUCCESS_TOPIC_ARN";
//    private const string FailureTopicArnEnvVariable = "FAILURE_TOPIC_ARN";

//    //private ChannelFactory<ILoggingService> _factory;

//    public string Profile { get; set; } = string.Empty;
//    //public static string Profile { get; set; } = string.Empty;
//    //public static string AccessKey { get; set; } = string.Empty;
//    //public static string SecretKey { get; set; } = string.Empty;
//    public string QueueName { get; set; } = string.Empty;
//    //public static string QueueName { get; set; } = string.Empty;
//    public string SuccessTopicArn { get; set; } = string.Empty;
//    //public static string SuccessTopicArn { get; set; } = string.Empty;
//    public string FailureTopicArn { get; set; } = string.Empty;
//    //public static string FailureTopicArn { get; set; } = string.Empty;
//    public IAmazonSQS SqsClient { get; }

//    public SQSEnvironmentFixture()
//    {
//        ReadTestEnvironmentSettingsFromFile(Path.Combine("SQS", "appsettings.test.json"));
//        SqsClient = new AmazonSQSClient(
//            CredentialsHelper.GetCredentials(
//                new AWSOptions 
//                {
//                    Profile = Profile
//                }));
//        //CreateAndStartHost();
//        //CreateAndOpenClientChannel();
//        //EnsureQueueIsEmpty();
//    }

//    private void EnsureQueueIsEmpty()
//    {
//        //var response = SqsClient.PurgeQueueAsync(QueueUrl).Result;
//        //response.Validate();
//    }

//    //public static AWSCredentials GetCredentials()
//    //{
//    //    return new BasicAWSCredentials(AccessKey, SecretKey);
//    //}

//    //public IAmazonSQS GetSqsClient()
//    //{
//    //    return new AmazonSQSClient(GetCredentials());
//    //}

//    //public string GetQueueName()
//    //{
//    //    return QueueName;
//    //}

//    private void ReadTestEnvironmentSettingsFromFile(string settingsFilePath)
//    {
//        var json = File.ReadAllText(settingsFilePath);
//        var appSettingsDictionary = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

//        var settingsDict = appSettingsDictionary[AwsKey];
//        Profile = settingsDict[ProfileEnvVariable];
//        //AccessKey = settingsDict[AccessKeyEnvVariable];
//        //SecretKey = settingsDict[SecretKeyEnvVariable];
//        QueueName = settingsDict[TestQueueNameEnvVariable];
//        SuccessTopicArn = settingsDict[SuccessTopicArnEnvVariable];
//        FailureTopicArn = settingsDict[FailureTopicArnEnvVariable];
//    }
//}
