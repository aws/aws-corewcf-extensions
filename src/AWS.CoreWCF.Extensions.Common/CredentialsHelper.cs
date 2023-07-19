using Amazon.Extensions.NETCore.Setup;
using Amazon.KeyManagementService.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace AWS.CoreWCF.Extensions.Common;

public class CredentialsHelper
{
    public static AWSCredentials GetCredentials(AWSOptions? options)
    {
        if (options?.Credentials != null)
        {
            return options.Credentials;
        }

        if (!string.IsNullOrEmpty(options?.Profile))
        {
            var chain = new CredentialProfileStoreChain(options.ProfilesLocation);
            if (chain.TryGetAWSCredentials(options.Profile, out var awsCredentials))
            {
                return awsCredentials;
            }
        }

        var credentials = FallbackCredentialsFactory.GetCredentials();
        if (credentials == null)
        {
            throw new NotFoundException("Failed to find AWS Credentials.");
        }
        return credentials;
    }
}
