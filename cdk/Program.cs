using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.CDK;
using Environment = System.Environment;

namespace AWS.CoreWCF.ServerExtensions.Cdk
{
    [ExcludeFromCodeCoverage]
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();

            new IntegrationTestsStack(
                app,
                "AWSCoreWCFServerExtensionsIntegrationTests",
                new StackProps
                {
                    // creds are defined in .gitlab-ci.yml

                    TerminationProtection = true
                }
            );

            new BenchmarkToolCdkStack(
                app,
                "AWSCoreWCFServerExtensionsBenchmarkStack",
                new StackProps
                {
                    // creds are defined in .gitlab-ci.yml

                    TerminationProtection = true
                }
            );

            new CodeSigningAndDeployStack(
                app,
                "AWSCoreWCFServerExtensionsCodeSigning",
                new CodeSigningAndDeployStackProps
                {
                    // creds are defined in .gitlab-ci.yml

                    // env vars are defined in gitlab settings
                    Signing = new CodeSigningAndDeployStackProps.SigningProps
                    {
                        SigningRoleArn = Environment.GetEnvironmentVariable("SIGNING_ROLE_ARN") ?? "signerRole",
                        SignedBucketName =
                            Environment.GetEnvironmentVariable("SIGNED_BUCKET_NAME") ?? "signedBucketArn",
                        UnsignedBucketName =
                            Environment.GetEnvironmentVariable("UNSIGNED_BUCKET_NAME") ?? "unsignedBucketArn",
                    },
                    NugetPublishing = new CodeSigningAndDeployStackProps.NugetPublishingProps
                    {
                        SecretArnCoreWCFNugetPublishKey =
                            Environment.GetEnvironmentVariable("SECRET_ARN_CORE_WCF_NUGET_PUBLISH_KEY") ?? "corewcf",
                        SecretArnWCFNugetPublishKey =
                            Environment.GetEnvironmentVariable("SECRET_ARN_WCF_NUGET_PUBLISH_KEY") ?? "wcf",
                        NugetPublishSecretAccessRoleArn =
                            Environment.GetEnvironmentVariable("NUGET_PUBLISH_SECRET_ACCESS_ROLE_ARN")
                            ?? "nugetPublishRoleArn",
                    },
                    TerminationProtection = true
                }
            );

            new CanaryMonitoringStack(
                app,
                "AWSCoreWCFServerExtensionsCanaryMonitoring",
                new CanaryMonitoringStackProps
                {
                    // creds are defined in .gitlab-ci.yml
                    CloudWatchDashboardServicePrincipalName =
                        Environment.GetEnvironmentVariable(
                            "CANARY_MONITORING_CLOUDWATCH_DASHBOARD_SERVICE_PRINCIPAL_NAME"
                        ) ?? "cloudWatchDashboardServicePrincipalName",
                    CloudWatchDashboardPolicyStatementId =
                        Environment.GetEnvironmentVariable("CANARY_MONITORING_CLOUDWATCH_DASHBOARD_POLICY_STATEMENT_ID")
                        ?? "CloudWatchDashboardPolicyStatementId",
                    TicketingArn =
                        Environment.GetEnvironmentVariable("CANARY_MONITORING_AWS_TICKETING_ARN_PREFIX")
                        ?? "ticketingArn",
                    TicketingCti = JsonSerializer.Deserialize<Cti>(
                        Environment.GetEnvironmentVariable("CANARY_MONITORING_AWS_TICKETING_CTI_JSON") ?? "{}"
                    ),
                    TerminationProtection = true
                }
            );

            app.Synth();
        }
    }
}
