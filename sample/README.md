## Sample

This contains a working sample demonstrating how to use AWS.CoreWCF.Extensions

### Pre-Requisites
- Setup your local environment with your [AWS Credentials](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-files.html).  One way to do this is by installing the [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-chap-getting-started.html) and running `aws configure`.

### Running

In a PowerShell/Terminal run:

```
dotnet run --project .\sample\Server\Server.csproj
```

In a **SEPARATE** PowerShell/Terminal run:

```
dotnet run --project .\sample\Client\Client.csproj
```

When prompted in the _Client_ shell, enter a message.  The _Server_ shell should then output the message you entered.