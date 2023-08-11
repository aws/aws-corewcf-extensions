using System.Reflection;
using Amazon.Runtime;

namespace AWS.CoreWCF.Extensions.Common;

public static class AmazonServiceExtensions
{
    private static readonly string UserAgentSuffix = $" CoreWCF-{Assembly.GetExecutingAssembly().GetName().Version?.ToString()}";
    private const string UserAgentHeader = "User-Agent";

    /// <summary>
    /// Modifies the User-Agent header for api requests made by the <paramref name="amazonServiceClient"/>
    /// to indicate the call was bade via CoreWCF.
    /// </summary>
    public static void SetCustomUserAgentSuffix(this AmazonServiceClient amazonServiceClient)
    {
        amazonServiceClient.BeforeRequestEvent += (sender, e) =>
        {
            if (e is not WebServiceRequestEventArgs args || !args.Headers.ContainsKey(UserAgentHeader))
                return;

            if (args.Headers[UserAgentHeader].EndsWith(UserAgentSuffix))
                return;

            args.Headers[UserAgentHeader] += UserAgentSuffix;
        };
    }
}