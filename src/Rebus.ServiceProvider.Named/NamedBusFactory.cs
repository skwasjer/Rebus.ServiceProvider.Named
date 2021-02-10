using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    internal class NamedBusFactory : INamedBusFactory, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, NamedBusOptions> _busOptions;
        private readonly Dictionary<string, BusInstance> _buses;

        public NamedBusFactory(IEnumerable<NamedBusOptions> busOptions, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _busOptions = busOptions?.ToDictionary(b => b.Name) ?? throw new ArgumentNullException(nameof(busOptions));
            _buses = new Dictionary<string, BusInstance>();
        }

        public IBus Get(string name) => ((NamedBusStarter)GetStarter(name)).Bus;

        public IBusStarter GetStarter(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_buses.TryGetValue(name, out BusInstance v))
            {
                return v.BusStarter;
            }

            if (_busOptions.TryGetValue(name, out NamedBusOptions busOptions))
            {
                return (_buses[name] = CreateBusStarter(busOptions)).BusStarter;
            }

            throw new InvalidOperationException($"Bus with name '{name}' does not exist.");
        }

        private BusInstance CreateBusStarter(NamedBusOptions busOptions)
        {
            // Use a new service container to mount this Rebus instance.
            IServiceCollection innerServices = new ServiceCollection()
                .AddRebus(configurer =>
                {
                    // Note: explicitly using outer service provider here.
                    return busOptions.ConfigureBus(configurer, _serviceProvider)
                        // Enforce bus name after all Rebus config has been applied.
                        .Options(o => o.SetBusName(busOptions.Name));
                });

            // We want to resolve handlers from the outer service container however
            // so replacing the default activator with our own that supports named buses.
            innerServices.Replace(ServiceDescriptor.Singleton<IHandlerActivator>(
                new NamedBusHandlerActivator(busOptions.Name, _serviceProvider.GetRequiredService<IHandlerActivator>())
            ));

            IServiceProvider innerServiceProvider = innerServices.BuildServiceProvider();
            IBusStarter busStarter = innerServiceProvider.GetRequiredService<IBusStarter>();
            return new BusInstance(innerServiceProvider, CreateBusStarter(busOptions, busStarter));
        }

        private static NamedBusStarter CreateBusStarter(NamedBusOptions busOptions, IBusStarter busStarter)
        {
            return new NamedBusStarter(busStarter, new NamedBus(busOptions.Name, busStarter.Bus));
        }

        public void Dispose()
        {
            foreach (BusInstance busInstance in _buses.Values)
            {
                busInstance.Dispose();
            }
        }

        private class BusInstance : IDisposable
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly NamedBusStarter _busStarter;

            public BusInstance(IServiceProvider serviceProvider, NamedBusStarter busStarter)
            {
                _serviceProvider = serviceProvider;
                _busStarter = busStarter;
            }

            public IBusStarter BusStarter => _busStarter;

            public void Dispose()
            {
                ((NamedBus)_busStarter.Bus).InnerBus.Dispose();
                (_serviceProvider as IDisposable)?.Dispose();
            }
        }
    }
}
