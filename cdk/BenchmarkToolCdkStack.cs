using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Amazon.CDK;
using Amazon.CDK.AWS.CodeBuild;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SecretsManager;
using Constructs;

namespace AWS.CoreWCF.ServerExtensions.Cdk
{
    [ExcludeFromCodeCoverage]
    public class BenchmarkToolCdkStack : Stack
    {
        internal BenchmarkToolCdkStack(Construct scope, string id, IStackProps props = null)
            : base(scope, id, props)
        {
            bool createCodeBuild = false;
            bool createGitHubUser = true;

            // Create Bucket to Store Benchmark Input, Output, and Results
            var coreWcfSqsBenchmarkBucket = new Bucket(
                this,
                "corewcf-sqs-benchmark-bucket",
                new BucketProps
                {
                    BlockPublicAccess = BlockPublicAccess.BLOCK_ACLS,
                    Versioned = false,
                    BucketName = $"corewcf-sqs-benchmark-bucket-{Account}",
#if DEBUG
                    AutoDeleteObjects = true,
                    RemovalPolicy = RemovalPolicy.DESTROY
#endif
                }
            );

            var benchmarkEC2Role = CreateEC2InstanceRole(coreWcfSqsBenchmarkBucket);

            var benchmarkEC2InstanceProfile = CreateEC2InstanceProfileAndRole(benchmarkEC2Role);

            if (createCodeBuild)
            {
                // Define a Role that Code Build job will use
                var codeBuildRole = new Role(
                    this,
                    "benchmarkingCodeBuildRole",
                    new RoleProps
                    {
                        AssumedBy = new ServicePrincipal("codebuild.amazonaws.com"),
                        RoleName = "sampleBenchmarkingCodeBuildRole"
                    }
                );

                var codeBuildJob = new Amazon.CDK.AWS.CodeBuild.Project(
                    this,
                    "sampleProject",
                    new ProjectProps
                    {
                        Role = codeBuildRole,
                        ProjectName = "Sample-Benchmark-CodeBuild-Project",
                        Environment = new BuildEnvironment
                        {
                            BuildImage = WindowsBuildImage.WIN_SERVER_CORE_2019_BASE_2_0,
                            ComputeType = WindowsBuildImage.WIN_SERVER_CORE_2019_BASE_2_0.DefaultComputeType,
                            EnvironmentVariables = new Dictionary<string, IBuildEnvironmentVariable>
                            {
                                {
                                    "BENCHMARK_BUCKET_NAME",
                                    new BuildEnvironmentVariable { Value = coreWcfSqsBenchmarkBucket.BucketName }
                                },
                                {
                                    "BENCHMARK_EC2_INSTANCE_PROFILE_ARN",
                                    new BuildEnvironmentVariable { Value = benchmarkEC2InstanceProfile.AttrArn }
                                },
                                {
                                    "DOTNET_CLI_TELEMETRY_OPTOUT",
                                    new BuildEnvironmentVariable { Value = "1" }
                                },
                                {
                                    "DOTNET_SKIP_FIRST_TIME_EXPERIENCE",
                                    new BuildEnvironmentVariable { Value = "true" }
                                }
                            }
                        },
                        BuildSpec = BuildSpec.FromAsset(GetBuildSpecPath()),
                        Source = Source.GitHub(
                            new GitHubSourceProps { Owner = "aws", Repo = "porting-assistant-dotnet-client" }
                        )
                    }
                );

                SetCodeBuildRolePolicies(codeBuildRole, codeBuildJob, coreWcfSqsBenchmarkBucket, benchmarkEC2Role);
                SetBenchmarkRolePolicies(
                    codeBuildRole,
                    (_, policy) => codeBuildRole.AddToPolicy(policy),
                    coreWcfSqsBenchmarkBucket,
                    benchmarkEC2Role
                );
            }

            AccessKey githubUserAccessKey = null;
            Secret githubUserAccessKeySecret = null;
            if (createGitHubUser)
            {
                var githubIamUser = new User(
                    this,
                    "corewcf-sqs-github-user",
                    new UserProps { UserName = "benchmarkingGitHubUser" }
                );

                githubUserAccessKey = new AccessKey(
                    this,
                    "corewcf-sqs-github-user-accesskey",
                    new AccessKeyProps { User = githubIamUser, Status = AccessKeyStatus.ACTIVE }
                );

                githubUserAccessKeySecret = new Secret(
                    this,
                    "corewcf-sqs-github-user-accesskey-secret",
                    new SecretProps { SecretStringValue = githubUserAccessKey.SecretAccessKey }
                );

                SetBenchmarkRolePolicies(
                    githubIamUser,
                    (_, policy) => githubIamUser.AddToPolicy(policy),
                    coreWcfSqsBenchmarkBucket,
                    benchmarkEC2Role
                );
            }

            GenerateCfnOutputs(
                coreWcfSqsBenchmarkBucket,
                benchmarkEC2InstanceProfile,
                githubUserAccessKey,
                githubUserAccessKeySecret
            );
        }

        internal Role CreateEC2InstanceRole(Bucket tuxnetBenchmarkBucket)
        {
            // Define a Role for dynamically created EC2 instances
            // (they need to be able to communicate with SSM)
            var benchmarkEC2Role = new Role(
                this,
                "benchmarkingEC2Role",
                new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                    RoleName = "tuxnetBenchmarkingDynamicEC2Role"
                }
            );

            // let EC2 write to tuxnetBenchmarkBucket
            tuxnetBenchmarkBucket.GrantReadWrite(benchmarkEC2Role);

            // let EC2 use SSM
            benchmarkEC2Role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"));

            return benchmarkEC2Role;
        }

        internal CfnInstanceProfile CreateEC2InstanceProfileAndRole(Role benchmarkEC2Role)
        {
            var instanceProfile = new CfnInstanceProfile(
                this,
                "benchmarkingEC2InstanceProfile",
                new CfnInstanceProfileProps
                {
                    InstanceProfileName = "benchmarkingEC2InstanceProfile",
                    Roles = new[] { benchmarkEC2Role.RoleName }
                }
            );

            return instanceProfile;
        }

        internal void SetCodeBuildRolePolicies(
            Role codeBuildRole,
            Project codeBuildJob,
            Bucket tuxnetBenchmarkBucket,
            Role benchmarkEC2Role
        )
        {
            // Let Code Build do all the Code Build things
            codeBuildRole.AddToPolicy(
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Actions = new[] { "logs:CreateLogGroup", "logs:CreateLogStream", "logs:PutLogEvents" },
                        Resources = new[] { "*" }
                    }
                )
            );

            // Let code Build log
            codeBuildRole.AddToPolicy(
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Actions = new[] { "codebuild:*" },
                        Resources = new[] { codeBuildJob.ProjectArn }
                    }
                )
            );
        }

        internal void SetBenchmarkRolePolicies<T>(
            T target,
            Action<T, PolicyStatement> addToPolicy,
            Bucket tuxnetBenchmarkBucket,
            Role benchmarkEC2Role
        )
            where T : IGrantable
        {
            // Let Code Build read/write to S3 Tuxnet Benchmark Bucket
            tuxnetBenchmarkBucket.GrantReadWrite(target);

            // Let Code Build create/delete EC2 instances
            addToPolicy(
                target,
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Actions = new[]
                        {
                            "ec2:RunInstances",
                            "ec2:CreateVolume",
                            "ec2:CreateSecurityGroup",
                            "ec2:CreateKeyPair",
                            "ec2:DeleteKeyPair",
                            "ec2:CreateTags",
                            "ec2:StartInstances",
                            "ec2:AssociateIamInstanceProfile",
                            "ec2:DescribeInstanceStatus",
                            "ssm:*"
                        },
                        Resources = new[] { "*" }
                    }
                )
            );
            addToPolicy(
                target,
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Actions = new[] { "ec2:StopInstances", "ec2:RebootInstances", "ec2:TerminateInstances" },
                        Resources = new[] { "*" },
                        // https://stackoverflow.com/questions/44693074/aws-iam-policy-allow-user-to-delete-only-the-ec2-instances-that-they-created
                        // only deleting ec2 instances tagged as TuxnetBenchmark
                        Conditions = new Dictionary<string, object>
                        {
                            {
                                "StringEquals",
                                new Dictionary<string, string> { { "aws:ResourceTag/TuxNetBenchmark", "true" } }
                            }
                        }
                    }
                )
            );
            // https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_use_passrole.html
            addToPolicy(
                target,
                new PolicyStatement(
                    new PolicyStatementProps
                    {
                        Actions = new[] { "iam:GetRole", "iam:PassRole" },
                        Resources = new[] { benchmarkEC2Role.RoleArn }
                    }
                )
            );
        }

        private string GetBuildSpecPath()
        {
            // TODO: Verify path is correct
            return Path.Combine(System.Environment.CurrentDirectory, "BenchmarkBuildSpec.json");
        }

        private void GenerateCfnOutputs(
            Bucket tuxnetBenchmarkBucket,
            CfnInstanceProfile benchmarkEC2InstanceProfile,
            AccessKey githubUserAccessKey,
            Secret githubUserAccessKeySecret
        )
        {
            new CfnOutput(
                this,
                "bucketName",
                new CfnOutputProps
                {
                    Value = tuxnetBenchmarkBucket.BucketName,
                    ExportName = "coreWcfSqsBenchmarkBucket"
                }
            );

            new CfnOutput(
                this,
                "roleArn",
                new CfnOutputProps { Value = benchmarkEC2InstanceProfile.AttrArn, ExportName = "benchmarkEC2RoleArn" }
            );

            if (null != githubUserAccessKey)
            {
                new CfnOutput(
                    this,
                    "accessKeyId",
                    new CfnOutputProps { Value = githubUserAccessKey.AccessKeyId, ExportName = "githubUserAccessKeyId" }
                );

                new CfnOutput(
                    this,
                    "accessKeySecret",
                    new CfnOutputProps
                    {
                        Value = githubUserAccessKeySecret.SecretValue.UnsafeUnwrap(),
                        ExportName = "githubUserAccessSecret"
                    }
                );
            }
        }
    }
}
