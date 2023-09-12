using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Environment = System.Environment;

namespace AWS.CoreWCF.ServerExtensions.Cdk
{
    [ExcludeFromCodeCoverage]
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var stackProps = new StackProps
            {
                // creds are defined in .gitlab-ci.yml

                TerminationProtection = true
            };

            var codeSigningProps = new CodeSigningAndDeployStackProps
            {
                // creds are defined in .gitlab-ci.yml

                // env vars are defined in gitlab settings
                Signing = new CodeSigningAndDeployStackProps.SigningProps
                {
                    SigningRoleArn = Environment.GetEnvironmentVariable("SIGNING_ROLE_ARN") ?? "signerRole",
                    SignedBucketName = Environment.GetEnvironmentVariable("SIGNED_BUCKET_NAME") ?? "signedBucketArn",
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
            };

            var app = new App();

            new IntegrationTestsStack(app, "AWSCoreWCFServerExtensionsIntegrationTests", stackProps);

            new CodeSigningAndDeployStack(app, "AWSCoreWCFServerExtensionsCodeSigning", codeSigningProps);

            app.Synth();
        }
    }
}
