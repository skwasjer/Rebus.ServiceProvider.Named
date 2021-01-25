using System;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    internal class NamedBusStarter : INamedBusStarter
    {
        private readonly IBusStarter _originalBusStarter;

        public NamedBusStarter(IBusStarter originalBusStarter, INamedBus namedBus)
        {
            _originalBusStarter = originalBusStarter ?? throw new ArgumentNullException(nameof(originalBusStarter));
            Bus = namedBus ?? throw new ArgumentNullException(nameof(namedBus));
        }

        public INamedBus Start()
        {
            _originalBusStarter.Start();
            return Bus;
        }

        public INamedBus Bus { get; }

        IBus IBusStarter.Bus => Bus;

        IBus IBusStarter.Start() => Start();
    }
}
