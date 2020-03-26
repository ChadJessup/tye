using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration.Tye
{
    public class TyeConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly TyeConfigurationSource source;
        private readonly HubConnection? connection;
        private readonly IDisposable? changeTokenRegistration;
        private readonly Task? openTask;

        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public TyeConfigurationProvider(TyeConfigurationSource tyeConfigurationSource)
        {
            this.source = tyeConfigurationSource;
            var hubUrl = Environment.GetEnvironmentVariable("TYE_SHARED_CONFIGURATION");
            Console.WriteLine($"TYE_SHARED_CONFIGURATION: {hubUrl}");

            if (string.IsNullOrWhiteSpace(hubUrl))
            {
                return;
            }

            // TODO: can I have a typed connection? Can't find it in the docs.
            this.connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .AddJsonProtocol()
                .ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddConsole();
                })
                .Build();

            this.connection.On<Dictionary<string, string>>(
                methodName: nameof(IConfigurationHub.SettingsChanged),
                handler: s =>
                {
                    Console.WriteLine($"New changes received!");

                    this.Data = s;
                });

            this.connection.Closed += e =>
            {
                Console.WriteLine($"Connection closed: {e.Message}");

                return Task.CompletedTask;
            };

            this.connection.Reconnected += s =>
            {
                Console.WriteLine($"Reconnected: {s}");

                return Task.CompletedTask;
            };

            this.connection.Reconnecting += e =>
            {
                Console.WriteLine($"Reconnecting: {e}");

                return Task.CompletedTask;
            };

            this.openTask = Task.Run(async () => await this.connection.StartAsync());

            if (this.source.ReloadOnChange)
            {
                changeTokenRegistration = ChangeToken.OnChange(
                   GetReloadToken,
                   Load);
            }
        }

        // Exposed for testing/debugging
        public override void Load()
        {
            base.Load();
        }

        /// <inheritdoc />
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Dispose the provider.
        /// </summary>
        /// <param name="disposing"><c>true</c> if invoked from <see cref="IDisposable.Dispose"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.connection != null && this.connection?.State != HubConnectionState.Disconnected)
            {
                Task.Run(async () => await this.connection!.StopAsync());
            }

            this.changeTokenRegistration?.Dispose();
        }

        /// <summary>
        /// Generates a string representing this provider name and relevant details.
        /// </summary>
        /// <returns> The configuration name.</returns>
        public override string ToString()
            => $"{GetType().Name} ({(source.Optional ? "Optional" : "Required")})";
    }
}
