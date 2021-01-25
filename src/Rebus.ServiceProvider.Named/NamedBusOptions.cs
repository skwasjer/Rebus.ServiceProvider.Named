using System;
using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    internal class NamedBusOptions
    {
        /// <summary>
        /// Gets or sets the bus name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The delegate to configure the bus.
        /// </summary>
        public Func<RebusConfigurer, IServiceProvider, RebusConfigurer> ConfigureBus { get; set; }
    }
}
