using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.SQS;
using Amazon.SQS.Model;
using AWS.CoreWCF.Extensions.Common;
using Shouldly;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests
{
    /// <summary>
    /// Negative tests for <see cref="AmazonServiceExtensions"/>
    /// </summary>
    public class AmazonServiceExtensionTests
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly WebServiceRequestEventArgs? _webServiceRequestEventArgs;
        private readonly RequestEventHandler? _eventHandler;

        public AmazonServiceExtensionTests()
        {
            // ARRANGE
            _sqsClient = new AmazonSQSClient(new AnonymousAWSCredentials(), RegionEndpoint.USWest2);

            _sqsClient.SetCustomUserAgentSuffix();

            var request = new DefaultRequest(new CreateQueueRequest("dummy"), _sqsClient.GetType().Name);

            _webServiceRequestEventArgs =
                typeof(WebServiceRequestEventArgs)
                    .GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Static)!
                    .Invoke(null, new object[] { request }) as WebServiceRequestEventArgs;

            _eventHandler =
                typeof(AmazonServiceClient)
                    .GetField(
                        "mBeforeRequestEvent",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField
                    )!
                    .GetValue(_sqsClient) as RequestEventHandler;
        }

        [Fact]
        public void DoesNotErrorOutWhenHeadersIsMissing()
        {
            // ARRANGE
            // done in constructor

            // ACT
            _eventHandler!.Invoke(_sqsClient, _webServiceRequestEventArgs);
            _eventHandler!.Invoke(_sqsClient, _webServiceRequestEventArgs);

            // ASSERT
            // no exception is thrown
        }

        [Fact]
        public void HeaderAdditionAlgorithmIsIdempotent()
        {
            // ARRANGE
            _webServiceRequestEventArgs!.Headers["User-Agent"] = "fake";

            // ACT
            _eventHandler!.Invoke(_sqsClient, _webServiceRequestEventArgs);
            _eventHandler!.Invoke(_sqsClient, _webServiceRequestEventArgs);

            // ASSERT
            _webServiceRequestEventArgs
                .Headers["User-Agent"]
                .Split(" ")
                .Count(s => s.StartsWith("ft/corewcf"))
                .ShouldBe(1);
        }
    }
}
