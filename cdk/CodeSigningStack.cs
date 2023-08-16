using System.Diagnostics.CodeAnalysis;
using Amazon.CDK;
using Constructs;

namespace AWS.CoreWCF.ServerExtensions.Cdk;

[ExcludeFromCodeCoverage]
public class CodeSigningStack : Stack
{
    internal CodeSigningStack(Construct scope, string id, IStackProps props = null)
        : base(scope, id, props) { }
}
