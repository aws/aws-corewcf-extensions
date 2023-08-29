using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;
using AWS.WCF.Extensions.SQS;
using Shouldly;
using Xunit;

namespace AWS.WCF.Extensions.Tests
{
    /// <summary>
    /// Negative tests for <see cref="AwsSqsTransportBindingElement"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AwsSqsTransportBindingElementTests
    {
        [Fact]
        public void GetPropertyThrowsArgumentNullException()
        {
            // ARRANGE
            var element = new AWS.WCF.Extensions.SQS.AwsSqsTransportBindingElement(null, null);

            Exception? expectedException = null;

            // ACT
            try
            {
                element.GetProperty<string>(null);
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            // ASSERT
            expectedException.ShouldNotBeNull();
            expectedException.ShouldBeOfType<ArgumentNullException>();
        }

        [Fact]
        public void BuildChannelFactoryThrowsArgumentNullException()
        {
            // ARRANGE
            var element = new AWS.WCF.Extensions.SQS.AwsSqsTransportBindingElement(null, null);

            Exception? expectedException = null;

            // ACT
            try
            {
                element.BuildChannelFactory<string>(null);
            }
            catch (Exception e)
            {
                expectedException = e;
            }

            // ASSERT
            expectedException.ShouldNotBeNull();
            expectedException.ShouldBeOfType<ArgumentNullException>();
        }
    }
}
