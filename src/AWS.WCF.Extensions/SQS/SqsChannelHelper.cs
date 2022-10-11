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

    // Ensure our advertised MessageVersion matches the version we're
    // using to serialize/deserialize data to/from the wire
    //internal static MessageVersion MessageVersion => _messageEncoderFactory.MessageVersion;
    
    internal static MessageEncoderFactory DefaultMessageEncoderFactory => _messageEncoderFactory;
}

//static class SqsConfigurationStrings
//{
//    public const string MaxBufferPoolSize = "maxBufferPoolSize";
//    public const string MaxReceivedMessageSize = "maxMessageSize";
//}

static class SqsDefaults
{
    internal const long MaxBufferPoolSize = 64 * 1024;
    internal const int MaxReceivedMessageSize = 262144; // Max size for SQS message is 262144 (2^18)
}
