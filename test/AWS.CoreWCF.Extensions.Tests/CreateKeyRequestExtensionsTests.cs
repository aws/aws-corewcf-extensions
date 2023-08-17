using Amazon.KeyManagementService.Model;
using AWS.CoreWCF.Extensions.Common;
using Xunit;

namespace AWS.CoreWCF.Extensions.Tests
{
    public class CreateKeyRequestExtensionsTests
    {
        [Fact]
        public void GeneratesBasicPolicy()
        {
            // ARRANGE
            const string fakeId = "1234567890";

            var request = new CreateKeyRequest();

            // ACT
            request.WithBasicPolicy(fakeId);

            // ASSERT
            Assert.Contains(fakeId, request.Policy);
        }
    }
}
