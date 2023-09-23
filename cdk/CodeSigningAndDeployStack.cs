using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.CodePipeline;
using Amazon.CDK.AWS.CodePipeline.Actions;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace AWS.CoreWCF.ServerExtensions.Cdk;

[ExcludeFromCodeCoverage]
public class CodeSigningAndDeployStackProps : StackProps
{
    public class SigningProps
    {
        public string SigningRoleArn { get; set; }
        public string UnsignedBucketName { get; set; }
        public string SignedBucketName { get; set; }
    }

    public class NugetPublishingProps
    {
        public string SecretArnCoreWCFNugetPublishKey { get; set; }
        public string SecretArnWCFNugetPublishKey { get; set; }
        public string NugetPublishSecretAccessRoleArn { get; set; }
    }

    public SigningProps Signing { get; set; }
    public NugetPublishingProps NugetPublishing { get; set; }
}

[ExcludeFromCodeCoverage]
public class CodeSigningAndDeployStack : Stack
{
    public static string GetInputBucketName(string account) => $"code-signing-and-deploy-pipeline-source-{account}";

    internal CodeSigningAndDeployStack(Construct scope, string id, CodeSigningAndDeployStackProps props = null)
        : base(scope, id, props)
    {
        var inputBucket = new Bucket(
            this,
            "codeSigningAndDeployPipelineSource",
            new BucketProps
            {
                BucketName = GetInputBucketName(Account),
                Versioned = true,
                BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
                EventBridgeEnabled = true
            }
        );

        new CfnOutput(
            this,
            "codeSigningAndDeployPipelineSourceBucketName",
            new CfnOutputProps
            {
                ExportName = "codeSigningAndDeployPipelineSourceBucketName",
                Value = inputBucket.BucketName
            }
        );

        var sourceOutput = new Artifact_("sourceOutput");
        var buildOutput = new Artifact_("buildOutput");
        var signOutput = new Artifact_("signOutput");

        var pipeline = new Pipeline(
            this,
            "codeSigningAndDeployPipeline",
            new PipelineProps
            {
                ArtifactBucket = new Bucket(this, "codeSigningAndDeployPipelineArtifacts"),
                PipelineName = "CoreWCF.SQS-SignAndDeploy"
            }
        );

        pipeline
            // source
            .AddS3Source(inputBucket, sourceOutput)
            // build
            .AddCodeBuildActionStage(
                "Build",
                new BuildEnvironment { ComputeType = ComputeType.LARGE, BuildImage = LinuxBuildImage.STANDARD_7_0 },
                BuildSpec.FromAsset("cdk/buildspecs/build.yml"),
                input: sourceOutput,
                output: buildOutput
            )
            // sign
            .AddCodeBuildActionStage(
                "Sign",
                new BuildEnvironment
                {
                    ComputeType = ComputeType.MEDIUM,
                    BuildImage = LinuxBuildImage.STANDARD_7_0,
                    EnvironmentVariables = props.Signing.ToBuildEnvironmentVariables()
                },
                BuildSpec.FromAsset("cdk/buildspecs/sign.yml"),
                input: buildOutput,
                output: signOutput
            )
            // approve
            .AddManualApprovalStage()
            // deploy
            .AddCodeBuildActionStage(
                "NugetDeploy",
                new BuildEnvironment
                {
                    ComputeType = ComputeType.MEDIUM,
                    BuildImage = LinuxBuildImage.STANDARD_7_0,
                    EnvironmentVariables = props.NugetPublishing.ToBuildEnvironmentVariables()
                },
                BuildSpec.FromAsset("cdk/buildspecs/nuget-deploy.yml"),
                input: signOutput,
                outputs: Array.Empty<Artifact_>()
            );
    }
}

[ExcludeFromCodeCoverage]
public static class PipelineBuilderExtensions
{
    public static Pipeline AddS3Source(this Pipeline pipeline, Bucket s3InputBucket, Artifact_ sourceOutput)
    {
        var s3SourceAction = new S3SourceAction(
            new S3SourceActionProps
            {
                ActionName = "Source",
                BucketKey = "AWSCoreWCFServerExtensions.zip",
                Bucket = s3InputBucket,
                Output = sourceOutput,
                Trigger = S3Trigger.POLL
            }
        );

        pipeline.AddStage(new StageOptions { StageName = "Source", Actions = new IAction[] { s3SourceAction } });

        return pipeline;
    }

    public static Pipeline AddCodeBuildActionStage(
        this Pipeline pipeline,
        string name,
        BuildEnvironment buildEnvironment,
        BuildSpec buildSpec,
        Artifact_ input,
        Artifact_ output
    )
    {
        return pipeline.AddCodeBuildActionStage(name, buildEnvironment, buildSpec, input, new[] { output });
    }

    public static Pipeline AddCodeBuildActionStage(
        this Pipeline pipeline,
        string name,
        BuildEnvironment buildEnvironment,
        BuildSpec buildSpec,
        Artifact_ input,
        Artifact_[] outputs
    )
    {
        var codeBuildProject = new Project(
            pipeline.Stack,
            $"{name.ToLower()}CodeBuild",
            new ProjectProps { Environment = buildEnvironment, BuildSpec = buildSpec }
        );

        // let code build call assume role on
        // other accounts
        codeBuildProject.Role.AddToPrincipalPolicy(
            new PolicyStatement(
                new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Actions = new[]
                    {
                        "sts:GetSessionToken",
                        "sts:AssumeRole",
                        "sts:TagSession",
                        "sts:GetCallerIdentity"
                    },
                    Resources = new[] { "*" }
                }
            )
        );

        pipeline.AddStage(
            new StageOptions
            {
                StageName = name,
                Actions = new IAction[]
                {
                    new CodeBuildAction(
                        new CodeBuildActionProps
                        {
                            Project = codeBuildProject,
                            ActionName = name,
                            Input = input,
                            Outputs = outputs
                        }
                    )
                }
            }
        );

        return pipeline;
    }

    public static Pipeline AddManualApprovalStage(this Pipeline pipeline)
    {
        pipeline.AddStage(
            new StageOptions
            {
                StageName = "ManualApproval",
                Actions = new IAction[]
                {
                    new ManualApprovalAction(new ManualApprovalActionProps { ActionName = "Approval" })
                }
            }
        );

        return pipeline;
    }
}

[ExcludeFromCodeCoverage]
public static class CodeSigningAndDeployStackPropsExtensions
{
    public static Dictionary<string, IBuildEnvironmentVariable> ToBuildEnvironmentVariables(
        this CodeSigningAndDeployStackProps.SigningProps props
    )
    {
        return new Dictionary<string, IBuildEnvironmentVariable>
        {
            {
                "SIGNING_ROLE_ARN",
                new BuildEnvironmentVariable
                {
                    Type = BuildEnvironmentVariableType.PLAINTEXT,
                    Value = props.SigningRoleArn
                }
            },
            {
                "SIGNED_BUCKET_NAME",
                new BuildEnvironmentVariable
                {
                    Type = BuildEnvironmentVariableType.PLAINTEXT,
                    Value = props.SignedBucketName
                }
            },
            {
                "UNSIGNED_BUCKET_NAME",
                new BuildEnvironmentVariable
                {
                    Type = BuildEnvironmentVariableType.PLAINTEXT,
                    Value = props.UnsignedBucketName
                }
            },
        };
    }

    public static Dictionary<string, IBuildEnvironmentVariable> ToBuildEnvironmentVariables(
        this CodeSigningAndDeployStackProps.NugetPublishingProps props
    )
    {
        return new Dictionary<string, IBuildEnvironmentVariable>
        {
            {
                "SECRET_ARN_CORE_WCF_NUGET_PUBLISH_KEY",
                new BuildEnvironmentVariable
                {
                    Type = BuildEnvironmentVariableType.PLAINTEXT,
                    Value = props.SecretArnCoreWCFNugetPublishKey
                }
            },
            {
                "SECRET_ARN_WCF_NUGET_PUBLISH_KEY",
                new BuildEnvironmentVariable
                {
                    Type = BuildEnvironmentVariableType.PLAINTEXT,
                    Value = props.SecretArnWCFNugetPublishKey
                }
            },
            {
                "NUGET_PUBLISH_SECRET_ACCESS_ROLE_ARN",
                new BuildEnvironmentVariable
                {
                    Type = BuildEnvironmentVariableType.PLAINTEXT,
                    Value = props.NugetPublishSecretAccessRoleArn
                }
            },
        };
    }
}
