using System;
using Rebus.Config;
using Rebus.Transport.InMem;

namespace Rebus.ServiceProvider.Named
{
    internal class MemoryBusConfigurationHelper
    {
        public static RebusConfigurer ConfigureForInMem(RebusConfigurer r) =>
            r.Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "queue"));

        public static RebusConfigurer ConfigureForInMemWithSp(RebusConfigurer r, IServiceProvider _) =>
            ConfigureForInMem(r);
    }
}
