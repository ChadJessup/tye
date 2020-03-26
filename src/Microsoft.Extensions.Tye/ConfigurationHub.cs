using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Configuration.Tye
{
    public class ConfigurationHub : Hub<IConfigurationHub>, IConfigurationHub
    {
        private readonly ILogger<ConfigurationHub> logger;
        private readonly IConfiguration configuration;

        public ConfigurationHub(ILogger<ConfigurationHub> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public override async Task OnConnectedAsync()
        {
            this.logger.LogInformation($"OnConnected");
            await base.OnConnectedAsync();

            await this.SettingsChanged(this.configuration.ToDictionary());
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            this.logger.LogInformation($"OnDisconnected");

            return base.OnDisconnectedAsync(exception);
        }

        public async Task SettingsChanged(Dictionary<string, string> settings)
        {
            await this.Clients.All.SettingsChanged(settings);
        }
    }
}
