// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Tye;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Tye.Hosting.Model;

namespace Microsoft.Tye.Hosting
{
    public class SharedConfigurationRunner : IApplicationProcessor
    {
        private readonly ILogger logger;
        private readonly bool shareConfigurations;
        private IHost? host;
        private string? hubUrl;
        private IDisposable? changeToken;

        public SharedConfigurationRunner(ILogger logger, SharedConfigurationOptions options)
        {
            this.logger = logger;
            shareConfigurations = options.ShareConfigurations;
        }

        private void GetConfiguration(WebHostBuilderContext context, IConfigurationBuilder builder, IEnumerable<string> serviceNames)
        {
            var env = context.HostingEnvironment;
            builder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("sharedSettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"sharedSettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

            foreach (var serviceName in serviceNames)
            {
                builder.AddJsonFile($"sharedSettings.{serviceName}.json", optional: true, reloadOnChange: true);
            }
        }

        public async Task StartAsync(Tye.Hosting.Model.Application application)
        {
            if (!this.shareConfigurations)
            {
                return;
            }

            static int GetNextPort()
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }

            var port = GetNextPort();

            this.host = new HostBuilder()
                .ConfigureWebHostDefaults(builder => builder
                    .ConfigureAppConfiguration((context, builder) => GetConfiguration(context, builder, application.Services.Keys.ToImmutableList()))
                    .UseKestrel(options => options.ListenAnyIP(port))
                    // TODO: chadj - 3/22/20 - anything built-in to chain this ILogger in with outer logger?
                    .ConfigureLogging(builder => builder.AddConsole())
                    .Configure(app => app
                        .UseRouting()
                        .UseEndpoints(endpoints => endpoints.MapHub<ConfigurationHub>("/configuration")))
                    .ConfigureServices(services => services
                        .AddSignalR(options => options.EnableDetailedErrors = true)
                        .AddJsonProtocol()))
                .Build();

            this.hubUrl = $"http://127.1:{port}/configuration";

            foreach (var s in application.Services)
            {
                s.Value.Description.Configuration.Add(
                    new ConfigurationSource("TYE_SHARED_CONFIGURATION", this.hubUrl));
            }

            if (host != null)
            {
                this.ObserveChanges(this.host.Services);
                await host.StartAsync();
            }
        }

        private void ObserveChanges(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            this.changeToken = configuration
                .GetReloadToken()
                .RegisterChangeCallback(ChangeDetected, null);
        }

        private void ChangeDetected(object state)
        {
            Console.WriteLine($"Change detected!");

            var config = this.host?.Services.GetRequiredService<IConfiguration>();
            var hub = this.host?.Services.GetRequiredService<IHubContext<ConfigurationHub, IConfigurationHub>>();

            hub!.Clients?.All?.SettingsChanged(config!.ToDictionary());

            this.changeToken = config!
                .GetReloadToken()
                .RegisterChangeCallback(ChangeDetected, null);
        }

        public async Task StopAsync(Tye.Hosting.Model.Application application)
        {
            if (host == null)
            {
                return;
            }

            await host.StopAsync();

            if (host is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }

            this.changeToken?.Dispose();
        }
    }
}
