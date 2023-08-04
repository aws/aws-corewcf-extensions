namespace AWS.WCF.Extensions.SQS;

static class SqsDefaults
{
    internal const long MaxBufferPoolSize = 64 * 1024;
    internal const int MaxSendMessageSize = 262144; // Max size for SQS message is 262144 (2^18)
}
