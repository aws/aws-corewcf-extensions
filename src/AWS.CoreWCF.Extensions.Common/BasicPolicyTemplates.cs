namespace AWS.CoreWCF.Extensions.Common;

public class BasicPolicyTemplates
{
    private const string AccountIdPlaceholder = "ACCOUNT_ID_PLACEHOLDER";
    private const string SQSArnPlaceholder = "SQS_ARN_PLACEHOLDER";
    public const string BasicSQSPolicyTemplate =
        $@"{{
  ""Version"": ""2008-10-17"",
  ""Id"": ""__default_policy_ID"",
  ""Statement"": [
    {{
      ""Sid"": ""__owner_statement"",
      ""Effect"": ""Allow"",
      ""Principal"": {{
        ""AWS"": ""{AccountIdPlaceholder}""
      }},
      ""Action"": [
        ""sqs:CreateQueue"",
        ""sqs:DeleteMessage"",
        ""sqs:GetQueueAttributes"",
        ""sqs:GetQueueUrl"",
        ""sqs:ReceiveMessage"",
        ""sqs:SendMessage"",
        ""sqs:SetQueueAttributes"",
        ""sqs:TagQueue""
      ],
      ""Resource"": ""{SQSArnPlaceholder}""
    }}
  ]
}}";

    public const string BasicKMSPolicyTemplate =
        $@"{{
  ""Version"": ""2008-10-17"",
  ""Id"": ""__default_policy_ID"",
  ""Statement"": [
    {{
      ""Sid"": ""Allow use of key"",
      ""Effect"": ""Allow"",
      ""Principal"": {{
        ""AWS"": [
            ""{AccountIdPlaceholder}""
        ]
      }},
      ""Action"": [
        ""kms:Encrypt"",
        ""kms:Decrypt"",
        ""kms:ReEncrypt*"",
        ""kms:GenerateDataKey*"",
        ""kms:DescribeKey""
      ],
      ""Resource"": ""*""
    }},
  ]
}}";

    public static string GetBasicSQSPolicy(string queueArn)
    {
        var accountId = GetAccountIdFromQueueArn(queueArn);
        return BasicSQSPolicyTemplate.Replace(AccountIdPlaceholder, accountId).Replace(SQSArnPlaceholder, queueArn);
    }

    private static string GetAccountIdFromQueueArn(string queueArn)
    {
        var arnParts = queueArn.Split(":");
        return arnParts[^2];
    }

    public static string GetBasicKMSPolicy(string accountId, IEnumerable<string>? accountIdsToAllow = null)
    {
        accountIdsToAllow ??= new List<string>();
        accountIdsToAllow = accountIdsToAllow.Append(accountId);

        var accountIdStrings = accountIdsToAllow.Select(id => @$"""{id}""");
        var joinedAccountIdsToAllow = string.Join(", ", accountIdStrings);

        return BasicKMSPolicyTemplate.Replace(AccountIdPlaceholder, joinedAccountIdsToAllow);
    }
}
