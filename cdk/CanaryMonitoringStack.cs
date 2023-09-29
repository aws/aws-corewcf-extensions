using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace AWS.CoreWCF.ServerExtensions.Cdk;

[ExcludeFromCodeCoverage]
public class CanaryMonitoringStackProps : StackProps
{
    public string CloudWatchDashboardServicePrincipalName { get; set; }
    public string CloudWatchDashboardPolicyStatementId { get; set; }

    /// <summary>
    /// Aws Internal ticketing routing
    /// </summary>
    public Cti TicketingCti { get; set; }

    /// <summary>
    /// Arn of the aws internal action that allows
    /// <see cref="AwsTicketingSystemAlarmAction"/> to
    /// create a ticket
    /// </summary>
    /// <remarks>
    /// Reference: [internal wiki]/bin/view/CloudWatchAlarms/Internal/CloudWatchAlarmsSIMTicketing
    /// </remarks>
    public string TicketingArn { get; set; }
}

/// <summary>
/// Infrastructure to monitor the tests run in ./.github/workflows/canary.yml
/// </summary>
[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
[ExcludeFromCodeCoverage]
public class CanaryMonitoringStack : Stack
{
    /// <summary>
    /// This must match the metric name used in ./.github/workflows/canary.yml
    /// </summary>
    /// <remarks>
    /// it's ok if the metric already exists, cdk wont actually create it
    /// https://stackoverflow.com/questions/74219411/creating-a-cloudwatch-alarm-on-an-existing-cloudwatch-metric-in-cdk
    /// </remarks>
    private const string CanaryMetricName = "corewcf-sqs-canary";
    private const string CanaryMetricNameSpace = "TuxNetOps";

    private readonly CanaryMonitoringStackProps _props;

    internal CanaryMonitoringStack(Construct scope, string id, CanaryMonitoringStackProps props)
        : base(scope, id, props)
    {
        _props = props;

        // reference the metric fired by the GitHub Canary
        var canaryDetectedFailedTestsMetric = new Metric(
            new MetricProps
            {
                Namespace = CanaryMetricNameSpace,
                MetricName = CanaryMetricName,
                Period = Duration.Minutes(15),
                Statistic = Stats.SUM
            }
        );

        // alarm if integration tests fail
        var canaryDetectedFailedTestsAlarm = CreateCanaryDetectedFailedTestsAlarm(canaryDetectedFailedTestsMetric);

        // alarm if we haven't received any data for the canary in last 24 hours
        var canaryDidNotRunAlarm = CreateCanaryDidNotRunAlarm(canaryDetectedFailedTestsMetric);

        CreateDashboard(canaryDetectedFailedTestsMetric, canaryDetectedFailedTestsAlarm, canaryDidNotRunAlarm);

        // enure the wiki can access the dashboard
        CreateDashboardWikiRole();
    }

    private IAlarm CreateCanaryDetectedFailedTestsAlarm(Metric canaryDetectedFailedTestsMetric)
    {
        const string dedupeString = "CanaryDetectedFailedTests";

        var canaryDetectedFailedTestsAlarm = new Alarm(
            this,
            "CanaryDetectedFailedTestsAlarm",
            new AlarmProps
            {
                AlarmName = "CoreWCF.SQS Canary Detected Failed Tests Alarm",
                AlarmDescription =
                    "The CoreWCF.SQS Canary Workflow detected Failed Tests.  See "
                    + "https://github.com/aws/aws-corewcf-extensions/actions/workflows/canary.yml",
                Metric = canaryDetectedFailedTestsMetric,
                ComparisonOperator = ComparisonOperator.LESS_THAN_THRESHOLD,
                Threshold = 1,
                EvaluationPeriods = 1,
                DatapointsToAlarm = 1,
                TreatMissingData = TreatMissingData.IGNORE
            }
        );

        // create a ticket in the event the alarm is triggered
        canaryDetectedFailedTestsAlarm.AddAlarmAction(
            new AwsTicketingSystemAlarmAction(
                new AwsTicketingSystemAlarmActionProps
                {
                    ArnPrefix = _props.TicketingArn,
                    Cti = _props.TicketingCti,
                    Severity = "3",
                    DedupeMessage = dedupeString
                }
            )
        );

        // alarm if we haven't had success for 1 days,
        var multipleFailureAlarm = new Alarm(
            this,
            "MultipleCanariesWithFailedTestsAlarm",
            new AlarmProps
            {
                AlarmName = "Multiple CoreWCF.SQS Canary Detected Failed Tests Alarm",
                AlarmDescription =
                    "Multiple failures detected by the CoreWCF.SQS Canary Workflow.  See "
                    + "https://github.com/aws/aws-corewcf-extensions/actions/workflows/canary.yml",
                Metric = canaryDetectedFailedTestsMetric.With(new MetricOptions { Period = Duration.Hours(24) }),
                ComparisonOperator = ComparisonOperator.LESS_THAN_THRESHOLD,
                Threshold = 1,
                EvaluationPeriods = 1,
                DatapointsToAlarm = 1,
                TreatMissingData = TreatMissingData.IGNORE
            }
        );

        // create a ticket in the event the multiple failure alarm is triggered
        multipleFailureAlarm.AddAlarmAction(
            new AwsTicketingSystemAlarmAction(
                new AwsTicketingSystemAlarmActionProps
                {
                    ArnPrefix = _props.TicketingArn,
                    Cti = _props.TicketingCti,
                    Severity = "2.5",
                    DedupeMessage = dedupeString
                }
            )
        );

        return canaryDetectedFailedTestsAlarm;
    }

    private IAlarm CreateCanaryDidNotRunAlarm(Metric canaryDetectedFailedTestsMetric)
    {
        var alarm = new Alarm(
            this,
            "canaryDidNotRunAlarm",
            new AlarmProps
            {
                AlarmName = "CoreWCF.SQS Canary Did Not Run Alarm",
                AlarmDescription =
                    "The CoreWCF.SQS Canary Workflow did not run to completion.  See "
                    + "https://github.com/aws/aws-corewcf-extensions/actions/workflows/canary.yml",
                Metric = new MathExpression(
                    new MathExpressionProps
                    {
                        Expression = "FILL(m1,0)",
                        Label = "Canary runs per day",
                        UsingMetrics = new Dictionary<string, IMetric> { { "m1", canaryDetectedFailedTestsMetric } },
                        Period = Duration.Hours(24)
                    }
                ),
                ComparisonOperator = ComparisonOperator.LESS_THAN_THRESHOLD,
                Threshold = 1,
                EvaluationPeriods = 1,
                DatapointsToAlarm = 1,
                TreatMissingData = TreatMissingData.BREACHING
            }
        );

        // create a ticket in the event the alarm is triggered
        alarm.AddAlarmAction(
            new AwsTicketingSystemAlarmAction(
                new AwsTicketingSystemAlarmActionProps
                {
                    ArnPrefix = _props.TicketingArn,
                    Cti = _props.TicketingCti,
                    Severity = "2.5"
                }
            )
        );

        return alarm;
    }

    private void CreateDashboard(
        Metric canaryDetectedFailedTestsMetric,
        IAlarm canaryDetectedFailedTestsAlarm,
        IAlarm canaryDidNotRunAlarm
    )
    {
        var canaryDashboard = new Dashboard(
            this,
            "canaryDashboard",
            new DashboardProps { DashboardName = "CoreWCF-SQS-Canary", DefaultInterval = Duration.Days(30) }
        );

        canaryDashboard.AddWidgets(
            new AlarmStatusWidget(
                new AlarmStatusWidgetProps
                {
                    Title = "CoreWCF.SQS Canary Alarms",
                    Alarms = new IAlarm[] { canaryDetectedFailedTestsAlarm, canaryDidNotRunAlarm }
                }
            )
        );

        canaryDashboard.AddWidgets(
            new GraphWidget(
                new GraphWidgetProps
                {
                    Left = new IMetric[]
                    {
                        canaryDetectedFailedTestsMetric.With(
                            new MetricOptions { Period = Duration.Days(1), Statistic = Stats.MINIMUM }
                        )
                    },
                    View = GraphWidgetView.TIME_SERIES,
                    Title = "CoreWCF.SQS Canary",
                    LeftYAxis = new YAxisProps
                    {
                        Label = "Success",
                        Min = 0,
                        Max = 1,
                        ShowUnits = false,
                    },
                    LeftAnnotations = new IHorizontalAnnotation[]
                    {
                        new HorizontalAnnotation
                        {
                            Color = "#d62728", // red
                            Label = "SLA",
                            Value = 0.995,
                            Fill = Shading.NONE
                        }
                    }
                }
            )
        );
    }

    private void CreateDashboardWikiRole()
    {
        // create the iam role to support viewing dashboards from our wiki
        new Role(
            this,
            "CloudWatchDashboardsRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal(_props.CloudWatchDashboardServicePrincipalName),
                RoleName = "CloudWatchDashboards",
                InlinePolicies = new Dictionary<string, PolicyDocument>
                {
                    {
                        "CloudwatchMetricAPIs",
                        new PolicyDocument(
                            new PolicyDocumentProps
                            {
                                Statements = new[]
                                {
                                    new PolicyStatement(
                                        new PolicyStatementProps
                                        {
                                            Actions = new[]
                                            {
                                                "cloudwatch:Describe*",
                                                "cloudwatch:Get*",
                                                "cloudwatch:List*",
                                                "cloudwatch:Search*",
                                                "ec2:DescribeTags"
                                            },
                                            Resources = new string[] { "*" },
                                            Effect = Effect.ALLOW,
                                            Sid = _props.CloudWatchDashboardPolicyStatementId
                                        }
                                    )
                                }
                            }
                        )
                    }
                }
            }
        );
    }
}
