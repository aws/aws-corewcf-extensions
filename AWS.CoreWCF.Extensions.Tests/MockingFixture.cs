using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using Moq;

namespace AWS.CoreWCF.Extensions.Tests;

public class MockingFixture
{
    public Mock<IServiceProvider> MockServiceProvider { get; set; }

    public MockingFixture()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
        //mockServiceProvider
        //    .Setup(x => x.GetService(typeof(IAmazonSimpleNotificationService)))
        //    .Returns(new ConfigurationDbContext(Options, StoreOptions));


        //var snsOutput = new List<string>();
        //var mockSns= new Mock<IAmazonSimpleNotificationService>();
        //mockSns.Setup(s => s.PublishAsync(It.IsAny<PublishRequest>(d => ))).ReturnsAsync()

        //VendorBriefController controller = new VendorBriefController(mock.Object);

        //VendorBrief brief = new VendorBrief();

        //controller.DeleteVendorBrief(brief);

        //mock.Verify(f => f.DeleteVendorBrief(brief));
        //mock.Verify(f => f.SaveChanges());
    }
}
