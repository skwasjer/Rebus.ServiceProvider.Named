﻿using System;
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
        private readonly Dictionary<string, (Microsoft.Extensions.DependencyInjection.ServiceProvider, NamedBusStarter)> _buses;

        public NamedBusFactory(IEnumerable<NamedBusOptions> busOptions, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _busOptions = busOptions?.ToDictionary(b => b.Name) ?? throw new ArgumentNullException(nameof(busOptions));
            _buses = new Dictionary<string, (Microsoft.Extensions.DependencyInjection.ServiceProvider, NamedBusStarter)>();
        }

        public IBus Get(string name) => ((NamedBusStarter)GetStarter(name)).Bus;

        public IBusStarter GetStarter(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (_buses.TryGetValue(name, out (Microsoft.Extensions.DependencyInjection.ServiceProvider, NamedBusStarter) v))
            {
                return v.Item2;
            }

            if (_busOptions.TryGetValue(name, out NamedBusOptions busOptions))
            {
                return (_buses[name] = CreateBusStarter(busOptions)).Item2;
            }

            throw new InvalidOperationException($"Bus with name '{name}' does not exist.");
        }

        private (Microsoft.Extensions.DependencyInjection.ServiceProvider innerServiceProvider, NamedBusStarter namedBusStarter) CreateBusStarter(NamedBusOptions busOptions)
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

            Microsoft.Extensions.DependencyInjection.ServiceProvider innerServiceProvider = innerServices.BuildServiceProvider();
            IBusStarter busStarter = innerServiceProvider.GetRequiredService<IBusStarter>();
            return (innerServiceProvider, CreateBusStarter(busOptions, busStarter));
        }

        private static NamedBusStarter CreateBusStarter(NamedBusOptions busOptions, IBusStarter busStarter)
        {
            return new NamedBusStarter(busStarter, new NamedBus(busOptions.Name, busStarter.Bus));
        }

        public void Dispose()
        {
            foreach ((Microsoft.Extensions.DependencyInjection.ServiceProvider s, NamedBusStarter bs) in _buses.Values)
            {
                ((NamedBus)bs.Bus).InnerBus.Dispose();
                s.Dispose();
            }
        }
    }
}
