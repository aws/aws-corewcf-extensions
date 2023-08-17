using System.Diagnostics.CodeAnalysis;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests;

public class CredentialsHelperTests
{
    [Fact]
    [ExcludeFromCodeCoverage]
    public void UseFallbackCredentialsFactory()
    {
        // ARRANGE
        var fakeCredential = new BasicAWSCredentials("accessKey", "secretKey");

        FallbackCredentialsFactory.CredentialsGenerators.Add(() => fakeCredential);

        var emptyAwsOptions = new AWSOptions { Profile = "NonExistingProfile" };

        // ACT

        AWSCredentials? returnedCredentials = null;
        // capture any exceptions thrown by the GetCredentials method
        Exception? capturedException = null;
        try
        {
            returnedCredentials = Common.CredentialsHelper.GetCredentials(emptyAwsOptions);
        }
        catch (Exception e)
        {
            capturedException = e;
        }

        // ASSERT
        Assert.NotNull(returnedCredentials);
        // make sure we didn't see any exceptions bubble up from Invoke()
        Assert.Null(capturedException);
    }
}
