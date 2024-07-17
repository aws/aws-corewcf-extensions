using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using AWS.Extensions.IntegrationTests.Common;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace AWS.Extensions.IntegrationTests.SQS.TestService;

[ExcludeFromCodeCoverage]
public static class ServiceHelper
{
    /// <summary>
    /// Takes care of the plumbing to initialize a Test WebServer
    /// for Integration Tests
    /// </summary>
    /// <param name="configureServices">
    /// Equivalent to the Startup classes ConfigureServices(<see cref="IServiceCollection"/> services) method.
    /// </param>
    /// <param name="configure">
    /// Equivalent to the Startup classes Configure(<see cref="IApplicationBuilder"/> app) method.
    /// </param>
    /// <param name="testOutputHelper">
    /// Pass this in to wire up a <see cref="XUnitLoggingProvider"/>
    /// </param>
    /// <returns></returns>
    public static IWebHostBuilder CreateServiceHost(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configure,
        ITestOutputHelper? testOutputHelper = null
    )
    {
        return WebHost
            .CreateDefaultBuilder(Array.Empty<string>())
            .ConfigureServices(services =>
            {
                services.AddSingleton(new ConfigureStartup { Configure = configure });

                if (null != testOutputHelper)
                    services.AddLogging(builder =>
                    {
                        builder.AddProvider(new XUnitLoggingProvider(testOutputHelper));
                    });

                configureServices(services);
            })
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
            .UseStartup<InfrastructureTestStartup>();
    }

    private class InfrastructureTestStartup
    {
        public void ConfigureServices(IServiceCollection services) { }

        public void Configure(IApplicationBuilder app)
        {
            var configureStartup = app.ApplicationServices.GetRequiredService<ConfigureStartup>();
            configureStartup.Configure?.Invoke(app);
        }
    }

    private class ConfigureStartup
    {
        public Action<IApplicationBuilder>? Configure { get; set; }
    }
}
