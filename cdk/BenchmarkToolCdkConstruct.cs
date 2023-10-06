using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;

namespace AWS.CoreWCF.ServerExtensions.Cdk
{
    public class BenchmarkToolCdkStackProps : IStackProps
    {
        public WebIdentityPrincipal GithubOidcIdentityPrincipal { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class BenchmarkToolCdkConstruct
    {
        internal BenchmarkToolCdkConstruct(Stack scope, BenchmarkToolCdkStackProps props)
        {
            // Create Bucket to Store Benchmark Input, Output, and Results
            var coreWcfSqsBenchmarkBucket = new Bucket(
                scope,
                "corewcf-sqs-benchmark-bucket",
                new BucketProps
                {
                    BlockPublicAccess = BlockPublicAccess.BLOCK_ACLS,
                    Versioned = false,
                    BucketName = $"corewcf-sqs-benchmark-bucket-{scope.Account}"
                }
            );

            var githubPerformanceTestRunnerRole = new Role(
                scope,
                "githubPerformanceTestRunnerRole",
                new RoleProps
                {
                    AssumedBy = props.GithubOidcIdentityPrincipal,
                    RoleName = "githubPerformanceTestRunnerRole",
                    MaxSessionDuration = Duration.Hours(1)
                }
            );

            var benchmarkEC2Role = CreateEC2InstanceRole(scope, coreWcfSqsBenchmarkBucket);

            // Let EC2 Role run Performance Tests
            benchmarkEC2Role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AdministratorAccess"));

            var benchmarkEC2InstanceProfile = CreateEC2InstanceProfileAndRole(scope, benchmarkEC2Role);

            SetBenchmarkRolePolicies(
                githubPerformanceTestRunnerRole,
                (_, policy) => githubPerformanceTestRunnerRole.AddToPolicy(policy),
                coreWcfSqsBenchmarkBucket,
                benchmarkEC2Role
            );

            GenerateCfnOutputs(
                scope,
                githubPerformanceTestRunnerRole,
                coreWcfSqsBenchmarkBucket,
                benchmarkEC2InstanceProfile
            );
        }

        internal Role CreateEC2InstanceRole(Stack scope, Bucket benchmarkBucket)
        {
            // Define a Role for dynamically created EC2 instances
            // (they need to be able to communicate with SSM)
            var benchmarkEC2Role = new Role(
                scope,
                "benchmarkingEC2Role",
                new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
                    RoleName = "tuxnetBenchmarkingDynamicEC2Role"
                }
            );

            // let EC2 write to benchmarkBucket
            benchmarkBucket.GrantReadWrite(benchmarkEC2Role);

            // let EC2 use SSM
            benchmarkEC2Role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore"));

            return benchmarkEC2Role;
        }

        internal CfnInstanceProfile CreateEC2InstanceProfileAndRole(Stack scope, Role benchmarkEC2Role)
        {
            var instanceProfile = new CfnInstanceProfile(
                scope,
                "benchmarkingEC2InstanceProfile",
                new CfnInstanceProfileProps
                {
                    InstanceProfileName = "benchmarkingEC2InstanceProfile",
                    Roles = new[] { benchmarkEC2Role.RoleName }
                }
            );

            return instanceProfile;
        }

        internal void SetBenchmarkRolePolicies<T>(
            T target,
            Action<T, PolicyStatement> addToPolicy,
            Bucket benchmarkResultsBucket,
            Role benchmarkEC2Role
        )
            where T : IGrantable
        {
            // Enable read/write to S3 Benchmark Results Bucket
            benchmarkResultsBucket.GrantReadWrite(target);

            // Enable create/delete EC2 instances
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

        private void GenerateCfnOutputs(
            Stack scope,
            Role githubPerformanceTestRunnerRole,
            Bucket benchmarkResultsBucket,
            CfnInstanceProfile benchmarkEC2InstanceProfile
        )
        {
            new CfnOutput(
                scope,
                "githubBenchmarkTestRunnerArn",
                new CfnOutputProps
                {
                    Value = githubPerformanceTestRunnerRole.RoleArn,
                    ExportName = "githubBenchmarkTestRunnerArn"
                }
            );

            new CfnOutput(
                scope,
                "coreWcfSqsBenchmarkResultsBucket",
                new CfnOutputProps
                {
                    Value = benchmarkResultsBucket.BucketName,
                    ExportName = "coreWcfSqsBenchmarkResultsBucket"
                }
            );

            new CfnOutput(
                scope,
                "benchmarkEC2RoleArn",
                new CfnOutputProps { Value = benchmarkEC2InstanceProfile.AttrArn, ExportName = "benchmarkEC2RoleArn" }
            );
        }
    }
}
