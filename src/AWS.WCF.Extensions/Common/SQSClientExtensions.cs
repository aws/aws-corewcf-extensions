using Amazon.Runtime;

namespace AWS.WCF.Extensions.Common;

public static class SQSClientExtensions
{
    /// <summary>
    /// Uses the response object to determine if the http request was successful.
    /// </summary>
    /// <param name="response">Response to validate with</param>
    /// <exception cref="HttpRequestException">Thrown if http request was unsuccessful</exception>
    public static void Validate(this AmazonWebServiceResponse response)
    {
        var statusCode = (int)response.HttpStatusCode;
        if (statusCode < 200 || statusCode >= 300)
            throw new HttpRequestException($"HttpStatusCode: {statusCode}");
    }
}
