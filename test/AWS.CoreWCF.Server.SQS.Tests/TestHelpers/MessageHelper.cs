using AWS.CoreWCF.Server.Common;

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
            <name>TestMessage</name>
        </{1}>
    </s:Body>
</s:Envelope>";
    

    public static async Task SendMessageToQueueAsync(string iServiceName, string actionName)
    {
        var client = SdkClientHelper.GetSqsClientInstance();
        await client.SendMessageAsync(EnvironmentCollectionFixture.GetTestQueueUrl(), GetTestMessage(iServiceName, actionName));
    }

    private static string GetTestMessage(string iServiceName, string actionName)
    {
        return string.Format(TestMessageTemplate, iServiceName, actionName);
    }

    private static Stream GetTestMessageAsStream(string iServiceName, string actionName)
    {
        return GetTestMessage(iServiceName, actionName).ToStream();
    }
}   