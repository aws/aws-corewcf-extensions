# AWS CoreWCF Server Extensions
[![Build and Deploy](https://github.com/aws/aws-corewcf-extensions/actions/workflows/build-and-deploy.yml/badge.svg)](https://github.com/aws/aws-corewcf-extensions/actions/workflows/build-and-deploy.yml)

AWS CoreWCF Extensions is a collection of extension libraries for CoreWCF and WCF that provide cloud-native binding for Amazon SQS.  

The AWS.CoreWCF.Extensions package contains async binding and transports for CoreWCF Services.

The AWS.WCF.Extensions package contains extensions for WCF Clients to send messages via a SQS transport.

## Package Status

| Package                                                                                      | NuGet Stable                                                                                     | Downloads                                                                                     |
|:---------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------:|:---------------------------------------------------------------------------------------------:|
| [AWS.CoreWCF.Extensions](https://www.nuget.org/packages/AWS.CoreWCF.Extensions/) | [![AWS.CoreWCF.Extensions](https://img.shields.io/nuget/v/AWS.CoreWCF.Extensions.svg)](https://www.nuget.org/packages/AWS.CoreWCF.Extensions/) | [![AWS.CoreWCF.Extensions](https://img.shields.io/nuget/dt/AWS.CoreWCF.Extensions)](https://www.nuget.org/packages/AWS.CoreWCF.Extensions/) |
| [AWS.WCF.Extensions](https://www.nuget.org/packages/AWS.WCF.Extensions/)                                 | [![AWS.WCF.Extensions](https://img.shields.io/nuget/v/AWS.WCF.Extensions.svg)](https://www.nuget.org/packages/AWS.WCF.Extensions/)                                 | [![AWS.WCF.Extensions](https://img.shields.io/nuget/dt/AWS.WCF.Extensions)](https://www.nuget.org/packages/AWS.WCF.Extensions/)

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
        new AwsSqsBinding(),
        queueUrl
    );
});

app.Run();
```

### CoreWCF.SQS Server Performance Tuning

Using Amazon SQS as a backplane for CoreWCF allows Servers to scale both horizontally and vertically.  Deploying a CoreWCF Server application to additional virtual machines or docker instances will increase overall message throughput, as well as improve resiliency, and does not require modification of the application code.  This is possible because Amazon SQS automatically handles multiple readers.

It's also possible to control the performance of an individual Server application.  Doing so may offer significant throughput or efficiency increases depending on the characteristics of your application and the host you're running on.

A Server can be configured to use additional threads to process incoming messages and increase total throughput by setting the `ConcurrencyLevel` in the `AwsSqsBinding` constructor.  Setting this to a higher value allows multiple threads to deserilaize and ingest incoming messages as well as go through various extensibility points before CoreWCF dispatches the message for execution by the Service.

_NOTE:_ This will only impact message ingestion and extensability.  It does not change the Service Dispatch concurrency strategy and will not change how many messages are processed by a Service concurrently.

_NOTE:_ Setting this value above 10 will have no meaningful impact.  CoreWCF.SQS will read up to 10 messages in a batch from an Amazon SQS queue, and all messages must be processed before another batch is requested.  Increasing the Concurrency Level above 10 will leave any excess threads without any work to do.

Below is an example of increasing ConcurrencyLevel to increase message throughput:

```
/// <summary>
/// Example of increasing ConcurrencyLevel
/// </summary>
public static void Main(string[] args)
{
    var inventoryServiceQueue = "inventory";

    var builder = WebApplication.CreateBuilder(args);

    // if needed, customize your aws credentials here,
    // otherwise it will default to searching ~\.aws
    var awsCredentials = new AWSOptions();

    builder.Services
        .AddDefaultAWSOptions(awsCredentials)
        .AddServiceModelServices()
        .AddQueueTransport()
        .AddSQSClient(inventoryServiceQueue);

    var app = builder.Build();

    var inventoryUrl = app.EnsureSqsQueue(inventoryServiceQueue);

    app.UseServiceModel(services =>
    {
        services.AddService<InventoryService>();
        services.AddServiceEndpoint<InventoryService, IInventoryService>(
            new AwsSqsBinding(concurrencyLevel: 6), // <----- increase concurrencyLevel
            inventoryUrl);
    });

    app.Run();
}
```

A Server can also be configured to listen to multiple Amazon SQS Queues.  If expected traffic on several queues is expected to be light, enable one process to multiple queues can increase compute density and reduce operating costs.

Below is an example of a single Server listening to multiple Queues

```
/// <summary>
/// Example of listening to multiple Queues
/// </summary>
public static void Main(string[] args)
{
    var inventoryServiceQueue = "inventory";
    var orderProcessingServiceQueue = "orderProcessing";

    var builder = WebApplication.CreateBuilder(args);

    // if needed, customize your aws credentials here,
    // otherwise it will default to searching ~\.aws
    var awsCredentials = new AWSOptions();

    builder.Services
        .AddDefaultAWSOptions(awsCredentials)
        .AddServiceModelServices()
        .AddQueueTransport()
        .AddSQSClient(inventoryServiceQueue) // <----- Add multiple SQS Clients
        .AddSQSClient(orderProcessingServiceQueue);

    var app = builder.Build();

    var inventoryUrl = app.EnsureSqsQueue(inventoryServiceQueue);
    var orderProcessingUrl = app.EnsureSqsQueue(orderProcessingServiceQueue);

    app.UseServiceModel(services =>
    {
        services.AddService<InventoryService>();
        services.AddServiceEndpoint<InventoryService, IInventoryService>(
            new AwsSqsBinding(), 
            inventoryUrl);

        // Add multiple Service / ServiceEndpoints
        services.AddService<OrderProcessing>();
        services.AddServiceEndpoint<OrderProcessing, IOrderProcessing>(
            new AwsSqsBinding(), 
            orderProcessingUrl);
    });

    app.Run();
}
```

## Getting Help

Please use these community resources for getting help. We use the GitHub issues
for tracking bugs and feature requests.

* If it turns out that you may have found a bug,
  please open an [issue](https://github.com/aws/aws-corewcf-extensions/issues/new).
  
  
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

