using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {
        public static Dictionary<string, string> ToDictionary(this IConfiguration configuration)
            => configuration
            .AsEnumerable()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
