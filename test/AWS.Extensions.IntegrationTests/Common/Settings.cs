using System.Diagnostics.CodeAnalysis;

namespace AWS.Extensions.IntegrationTests.Common;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Settings
{
    public AWSSettings AWS { get; set; } = new();

    public class AWSSettings
    {
        //public string? PROFILE { get; set; }
        public string? AWS_ACCESS_KEY_ID { get; set; }
        public string? AWS_SECRET_ACCESS_KEY { get; set; }
        public string? AWS_REGION { get; set; } = "us-west-2";
        public string? FAILURE_TOPIC_ARN { get; set; }
        public string? SUCCESS_TOPIC_ARN { get; set; }
        public string? TEST_QUEUE_NAME { get; set; }
    }
}
