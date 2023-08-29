using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using AWS.Extensions.IntegrationTests.SQS.TestHelpers;
using Constructs;

namespace AWS.CoreWCF.ServerExtensions.Cdk;

/// <summary>
/// Infrastructure to run <see cref="AWS.Extensions.IntegrationTests"/>
/// </summary>
[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
[ExcludeFromCodeCoverage]
public class IntegrationTestsStack : Stack
{
    internal IntegrationTestsStack(Construct scope, string id, IStackProps props = null)
        : base(scope, id, props)
    {
        new Queue(
            this,
            "defaultSettingsQueue",
            new QueueProps { QueueName = ClientAndServerFixture.QueueWithDefaultSettings }
        );

        new Queue(
            this,
            "fifoQueue",
            new QueueProps
            {
                QueueName = ClientAndServerFixture.FifoQueueName,
                Fifo = true,
                ContentBasedDeduplication = true
            }
        );

        var snsSuccessQueue = new Queue(
            this,
            "snsSuccessQueue",
            new QueueProps { QueueName = ClientAndServerFixture.SnsNotificationSuccessQueue, }
        );

        var snsSuccessTopic = new Amazon.CDK.AWS.SNS.Topic(
            this,
            "snsSuccessTopic",
            new TopicProps { TopicName = "CoreWCF-Success" }
        );
        snsSuccessTopic.AddSubscription(new SqsSubscription(snsSuccessQueue));

        var snsFailureTopic = new Amazon.CDK.AWS.SNS.Topic(
            this,
            "snsFailureTopic",
            new TopicProps { TopicName = "CoreWCF-Failure" }
        );

        new CfnOutput(
            this,
            "snsSuccessTopicArn",
            new CfnOutputProps { ExportName = "SNS-Success-Topic-ARN", Value = snsSuccessTopic.TopicArn }
        );

        new CfnOutput(
            this,
            "snsFailureTopicArn",
            new CfnOutputProps { ExportName = "SNS-Failure-Topic-ARN", Value = snsFailureTopic.TopicArn }
        );
    }
}
