using System.ServiceModel;
using Amazon.SQS;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var queueName = "sample-sqs-queue";

            var sqsClient = new AmazonSQSClient();

            var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(sqsClient, queueName);
            var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
            var factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
            var channel = factory.CreateChannel();
            ((System.ServiceModel.Channels.IChannel)channel).Open();

            while (true)
            {
                Console.Write("Enter Message: ");
                var msg = Console.ReadLine() ?? "";

                Console.WriteLine();
                Console.WriteLine("Sending: " + msg);

                channel.LogMessage(msg);
                Console.WriteLine();
            }
        }
    }
}
