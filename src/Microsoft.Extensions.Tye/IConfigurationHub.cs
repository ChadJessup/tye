using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Configuration.Tye
{
    public interface IConfigurationHub
    {
        Task SettingsChanged(Dictionary<string, string> settings);
    }
}
