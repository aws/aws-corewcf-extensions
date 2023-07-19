using System.ServiceModel.Channels;

namespace AWS.WCF.Extensions.SQS;

internal static class SqsConstants
{
    internal const string Scheme = "https";

    private static readonly MessageEncoderFactory _messageEncoderFactory;

    static SqsConstants()
    {
        _messageEncoderFactory = new TextMessageEncodingBindingElement().CreateMessageEncoderFactory();
    }

    internal static MessageEncoderFactory DefaultMessageEncoderFactory => _messageEncoderFactory;
}

static class SqsDefaults
{
    internal const long MaxBufferPoolSize = 64 * 1024;
    internal const int MaxSendMessageSize = 262144; // Max size for SQS message is 262144 (2^18)
}
