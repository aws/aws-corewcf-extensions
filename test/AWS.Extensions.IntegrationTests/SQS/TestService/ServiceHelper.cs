// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;
using System.Net;

namespace AWS.Extensions.IntegrationTests.SQS.TestService;

public static class ServiceHelper
{
    public static IWebHostBuilder CreateServiceHost<TStartup>() where TStartup : class =>
        WebHost.CreateDefaultBuilder(Array.Empty<string>())
            .UseKestrel(options =>
            {
                options.Limits.MaxRequestBufferSize = null;
                options.Limits.MaxRequestBodySize = null;
                options.Limits.MaxResponseBufferSize = null;
                options.AllowSynchronousIO = true;
                options.Listen(IPAddress.Any, 8088, listenOptions =>
                {
                    //listenOptions.UseHttps();
                    if (Debugger.IsAttached)
                    {
                        listenOptions.UseConnectionLogging();
                    }
                });
                //options.Listen(IPAddress.Loopback, 8080, listenOptions =>
                //{
                //    if (Debugger.IsAttached)
                //    {
                //        listenOptions.UseConnectionLogging();
                //    }
                //});
            })
            .UseStartup<TStartup>();
}
