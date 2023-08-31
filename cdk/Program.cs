using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;

namespace AWS.CoreWCF.ServerExtensions.Cdk
{
    [ExcludeFromCodeCoverage]
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var sampleAWSCredToTestTruffleHog = "AKIAYVP4CIPPERUVIFXG";

            var stackProps = new StackProps
            {
                // creds are defined in .gitlab-ci.yml

                TerminationProtection = true
            };

            var app = new App();

            new IntegrationTestsStack(app, "AWSCoreWCFServerExtensionsIntegrationTests", stackProps);

            new CodeSigningStack(app, "AWSCoreWCFServerExtensionsCodeSigning", stackProps);

            app.Synth();
        }
    }
}
