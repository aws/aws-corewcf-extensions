using Amazon.KeyManagementService.Model;

namespace AWS.CoreWCF.Extensions.Common;

public static class CreateKeyRequestExtensions
{
    public static CreateKeyRequest WithBasicPolicy(
        this CreateKeyRequest request,
        string accountId,
        IEnumerable<string>? accountIdsToAllow = null
    )
    {
        var basicKMSPolicy = BasicPolicyTemplates.GetBasicKMSPolicy("", accountIdsToAllow);
        request.Policy = basicKMSPolicy;

        return request;
    }
}
