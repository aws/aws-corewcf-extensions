namespace AWS.CoreWCF.Extensions.Common;

public class BasicPolicyTemplates
{
    private const string AccountIdPlaceholder = "ACCOUNT_ID_PLACEHOLDER";
    private const string SQSArnPlaceholder = "SQS_ARN_PLACEHOLDER";

    /// <remarks>
    /// Must use "sqs:*" to get around limit of 7 actions in IAM Policies.
    /// Currently, needs 8 actions.
    /// </remarks>
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
        ""sqs:*""
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
        var arnParts = queueArn.Split(':');
        // get 2nd from the end
        return arnParts.Reverse().Skip(1).First();
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
