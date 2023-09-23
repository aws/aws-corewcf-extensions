# AWS CoreWCF Server Extensions
[![Build and Deploy](https://github.com/aws/aws-corewcf-extensions/actions/workflows/build-and-deploy.yml/badge.svg)](https://github.com/aws/aws-corewcf-extensions/actions/workflows/build-and-deploy.yml)

AWS CoreWCF Extensions is a collection of extension libraries for CoreWCF and WCF that provide cloud-native binding for Amazon SQS.  

The AWS.CoreWCF.Extensions package contains async binding and transports for CoreWCF Services.

The AWS.WCF.Extensions package contains extensions for WCF Clients to send messages via a SQS transport.

## Getting Started

For a Full Example, see the [sample](./sample/README.md) app.
### WCF Client

* Add [**AWS.WCF.Extensions**](https://www.nuget.org/packages/AWS.WCF.Extensions) to your project as a Nuget Package.
* Follow the example below to see how the library can be integrated into your application for to use SQS as the transport layer for a service.

```csharp
// this example assumes an existing wCF Service called ILoggingService

var sqsClient = new AmazonSQSClient();

var sqsBinding = new AWS.WCF.Extensions.SQS.AwsSqsBinding(sqsClient, queueName);
var endpointAddress = new EndpointAddress(new Uri(sqsBinding.QueueUrl));
var factory = new ChannelFactory<ILoggingService>(sqsBinding, endpointAddress);
var channel = factory.CreateChannel();
((System.ServiceModel.Channels.IChannel)channel).Open();

// send a message via the wcf client
channel.LogMessage("Hello World");
```

### CoreWCF Server

* Add [**AWS.CoreWCF.Extensions**](https://www.nuget.org/packages/AWS.CoreWCF.Extensions) to your project as a Nuget Package.
* Follow the example below to see how the library can be integrated into your application for to use SQS as the transport layer for a service.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDefaultAWSOptions(new AWSOptions())
    .AddServiceModelServices()
    .AddQueueTransport()
    .AddSQSClient(_queueName);

var app = builder.Build();

var queueUrl = app.EnsureSqsQueue(_queueName);

app.UseServiceModel(services =>
{
    services.AddService<LoggingService>();
    services.AddServiceEndpoint<LoggingService, ILoggingService>(
        new AwsSqsBinding
        {
            QueueName = _queueName
        },
        queueUrl
    );
});

app.Run();
```

## Getting Help

Please use these community resources for getting help. We use the GitHub issues
for tracking bugs and feature requests.

* If it turns out that you may have found a bug,
  please open an [issue](https://github.com/aws/aws-corewcf-extensions/issues/new)
  
  
## How to use this code?
* Clone the Git repository.
* Compile by running `dotnet build .`
* Edit the solution by opening `AWS.CoreWCF.Extensions.sln` using Visual Studio or Rider.


## Contributing

* We welcome community contributions and pull requests. See
[CONTRIBUTING](./CONTRIBUTING.md) for information on how to set up a development
environment and submit code.

# Additional Resources
 
- [CoreWCF](https://github.com/CoreWCF/CoreWCF)
- [WCF](https://github.com/dotnet/wcf)
- [Amazon SQS](https://aws.amazon.com/sqs/)

# License

Libraries in this repository are licensed under the Apache 2.0 License.

See [LICENSE](./LICENSE) and [NOTICE](./NOTICE) for more information.  

