namespace AWS.CoreWCF.Server.SQS.Tests.TestHelpers;

public class MessageHelper
{
    private const string TestMessageTemplate = @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing"">
    <s:Header>
        <a:Action s:mustUnderstand=""1"">http://tempuri.org/{0}/{1}</a:Action>
        <VsDebuggerCausalityData xmlns=""http://schemas.microsoft.com/vstudio/diagnostics/servicemodelsink"">
            uIDPo4IM/u5L8tFGizKmemV7kmEAAAAAcStFY6F8ukKPmzryw1DTrsJZH/CtAKlEjIDYla6EnwYACQAA
        </VsDebuggerCausalityData>
    </s:Header>
    <s:Body>
        <{1} xmlns=""http://tempuri.org/"">
            <toLog>{2}</toLog>
        </{1}>
    </s:Body>
</s:Envelope>";
    
    public static async Task SendMessageToQueueAsync(string iServiceName, string actionName, string messageId)
    {
        var client = SdkClientHelper.GetSqsClientInstance();
        await client.SendMessageAsync(EnvironmentCollectionFixture.GetTestQueueUrl(), FormatTestMessage(iServiceName, actionName, messageId));
    }

    private static string FormatTestMessage(string iServiceName, string actionName, string messageId)
    {
        return string.Format(TestMessageTemplate, iServiceName, actionName, messageId);
    }
}   