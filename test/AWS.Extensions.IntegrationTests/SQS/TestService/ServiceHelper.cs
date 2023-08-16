using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace AWS.Extensions.IntegrationTests.SQS.TestService;

[ExcludeFromCodeCoverage]
public static class ServiceHelper
{
    public static IWebHostBuilder CreateServiceHost<TStartup>(Action<IServiceCollection>? configureAction = null)
        where TStartup : class =>
        WebHost
            .CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureServices(configureAction ?? (_ => { }))
            .UseKestrel(options =>
            {
                options.Limits.MaxRequestBufferSize = null;
                options.Limits.MaxRequestBodySize = null;
                options.Limits.MaxResponseBufferSize = null;
                options.AllowSynchronousIO = true;
                options.Listen(
                    IPAddress.Any,
                    8088,
                    listenOptions =>
                    {
                        if (Debugger.IsAttached)
                        {
                            listenOptions.UseConnectionLogging();
                        }
                    }
                );
            })
            .UseStartup<TStartup>();
}
