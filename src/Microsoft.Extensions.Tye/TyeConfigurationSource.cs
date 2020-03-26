using Microsoft.AspNetCore.Builder;

namespace Microsoft.Extensions.Configuration.Tye
{
    public class TyeConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Determines if loading the file is optional.
        /// </summary>
        public bool Optional { get; set; }

        /// <summary>
        /// Determines whether the source will be loaded if the underlying file changes.
        /// </summary>
        public bool ReloadOnChange { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new TyeConfigurationProvider(this);
        }
    }
}
