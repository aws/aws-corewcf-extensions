using System.Diagnostics.CodeAnalysis;
using System.ServiceModel.Channels;

namespace AWS.WCF.Extensions.SQS;

[ExcludeFromCodeCoverage]
internal static class SqsConstants
{
    internal const string Scheme = "https";
    
    internal static MessageEncoderFactory DefaultMessageEncoderFactory { get; } = new TextMessageEncodingBindingElement().CreateMessageEncoderFactory();
}