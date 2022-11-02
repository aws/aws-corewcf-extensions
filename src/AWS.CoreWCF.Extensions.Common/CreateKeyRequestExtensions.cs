using Amazon.KeyManagementService.Model;
using AWS.CoreWCF.Extensions.Common;

namespace AWS.CoreWCF.Extensions.SQS.Infrastructure;

public static class CreateKeyRequestExtensions
{
    public static CreateKeyRequest WithBasicPolicy(this CreateKeyRequest request, string accountId, IEnumerable<string>? accountIdsToAllow = null)
    {
        var basicKMSPolicy = BasicPolicyTemplates.GetBasicKMSPolicy("", accountIdsToAllow);
        request.Policy = basicKMSPolicy;

        return request;
    }
}
