using AWS.CoreWCF.Extensions.SQS.DispatchCallbacks;

namespace AWS.CoreWCF.Extensions.Tests.SQS;

public class DispatchCallbacksTests
{
    [Fact]
    public void GetNullCallback_Returns_NullCallback_Function()
    {
        var nullCallback = DispatchCallbackFactory.GetNullCallback();
        Assert.Contains("_NullCallback", nullCallback.Method.Name);
    }
}
