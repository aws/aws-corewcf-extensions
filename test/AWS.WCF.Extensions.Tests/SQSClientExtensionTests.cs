using System.Diagnostics.CodeAnalysis;
using System.Net;
using Amazon.Runtime;
using AWS.WCF.Extensions.Common;
using Shouldly;
using Xunit;

namespace AWS.WCF.Extensions.Tests;

public class SQSClientExtensionTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Processing)]
    [ExcludeFromCodeCoverage]
    public void ValidateThrowsException(HttpStatusCode errorCode)
    {
        // ARRANGE
        var fakeAmazonWebServiceResponse = new AmazonWebServiceResponse { HttpStatusCode = errorCode };

        Exception? expectedException = null;

        // ACT
        try
        {
            fakeAmazonWebServiceResponse.Validate();
        }
        catch (Exception e)
        {
            expectedException = e;
        }

        // ASSERT
        expectedException.ShouldNotBeNull();
        expectedException.ShouldBeOfType<HttpRequestException>();
    }
}
