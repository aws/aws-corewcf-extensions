using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using AWS.Extensions.IntegrationTests.SQS;
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
        CreateGitHubOidcTestRunner();

        CreateSQSReadOnlyRole();

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
            new TopicProps { TopicName = ClientAndServerFixture.SuccessTopicName }
        );
        snsSuccessTopic.AddSubscription(new SqsSubscription(snsSuccessQueue));

        var snsFailureTopic = new Amazon.CDK.AWS.SNS.Topic(
            this,
            "snsFailureTopic",
            new TopicProps { TopicName = ClientAndServerFixture.FailureTopicName }
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

    /// <remarks>
    /// GitHub Documentation: https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-amazon-web-services
    /// Example Blog: https://towardsthecloud.com/aws-cdk-openid-connect-github
    /// </remarks>
    private void CreateGitHubOidcTestRunner()
    {
        var githubProvider = new OpenIdConnectProvider(
            this,
            "githubProvider",
            new OpenIdConnectProviderProps
            {
                Url = "https://token.actions.githubusercontent.com",
                ClientIds = new[] { "sts.amazonaws.com" }
            }
        );

        var assumeRoleIdentity = new WebIdentityPrincipal(
            githubProvider.OpenIdConnectProviderArn,
            conditions: new Dictionary<string, object>
            {
                {
                    "StringLike",
                    new Dictionary<string, string>
                    {
                        { "token.actions.githubusercontent.com:sub", "repo:aws/aws-corewcf-extensions:*" },
                        { "token.actions.githubusercontent.com:aud", "sts.amazonaws.com" }
                    }
                }
            }
        );

        var githubTestRunnerRole = new Role(
            this,
            "githubIntegrationTestRunnerRole",
            new RoleProps
            {
                AssumedBy = assumeRoleIdentity,
                ManagedPolicies = new[] { ManagedPolicy.FromAwsManagedPolicyName("AdministratorAccess") },
                RoleName = "corewcfGithubIntegrationTestRole",
                MaxSessionDuration = Duration.Hours(1)
            }
        );

        new CfnOutput(
            this,
            "githubIntegrationTestRunnerRoleArn",
            new CfnOutputProps
            {
                Value = githubTestRunnerRole.RoleArn,
                ExportName = "githubIntegrationTestRunnerRoleArn"
            }
        );
    }

    private void CreateSQSReadOnlyRole()
    {
        var role = new Role(
            this,
            NegativeIntegrationTests.SqsReadOnlyRoleName,
            new RoleProps
            {
                AssumedBy = new AccountPrincipal(base.Account),
                ManagedPolicies = new[] { ManagedPolicy.FromAwsManagedPolicyName("AmazonSQSReadOnlyAccess") },
                RoleName = NegativeIntegrationTests.SqsReadOnlyRoleName,
                MaxSessionDuration = Duration.Hours(1)
            }
        );

        role.AssumeRolePolicy.AddStatements(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Principals = new IPrincipal[] { new AccountPrincipal(base.Account) },
                    Actions = new[] { "sts:TagSession" }
                }
            )
        );
    }
}
