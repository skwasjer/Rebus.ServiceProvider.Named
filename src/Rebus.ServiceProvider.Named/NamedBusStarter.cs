using System;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    internal class NamedBusStarter : IBusStarter
    {
        private readonly IBusStarter _originalBusStarter;

        public NamedBusStarter(IBusStarter originalBusStarter, IBus namedBus)
        {
            _originalBusStarter = originalBusStarter ?? throw new ArgumentNullException(nameof(originalBusStarter));
            Bus = namedBus ?? throw new ArgumentNullException(nameof(namedBus));
        }

        public IBus Bus { get; }

        public IBus Start()
        {
            _originalBusStarter.Start();
            return Bus;
        }
    }
}
