using System.Diagnostics.CodeAnalysis;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.JSII.Runtime.Deputy;
using Constructs;

namespace AWS.CoreWCF.ServerExtensions.Cdk;

[ExcludeFromCodeCoverage]
public class AwsTicketingSystemAlarmAction : DeputyBase, IAlarmAction
{
    private readonly string _alarmActionArn;

    public AwsTicketingSystemAlarmAction(AwsTicketingSystemAlarmActionProps props)
    {
        _alarmActionArn = Encode(
            $"{props.ArnPrefix}:{props.Severity}:{props.Cti.Category}:{props.Cti.Type}:{props.Cti.Item}:{props.Cti.ResolverGroup}"
        );
    }

    public IAlarmActionConfig Bind(Construct scope, IAlarm alarm)
    {
        return new AlarmActionConfig { AlarmActionArn = _alarmActionArn };
    }

    private string Encode(string value)
    {
        return value.Replace(' ', '+');
    }
}

[ExcludeFromCodeCoverage]
public class AwsTicketingSystemAlarmActionProps
{
    public string ArnPrefix { get; set; }
    public string Severity { get; set; }
    public Cti Cti { get; set; }
}

[ExcludeFromCodeCoverage]
public class Cti
{
    public string Category { get; set; }
    public string Type { get; set; }
    public string Item { get; set; }
    public string ResolverGroup { get; set; }
}
