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
    internal sealed class NamedBusFactory : INamedBusFactory, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, NamedBusOptions> _busOptions;
        private readonly Dictionary<string, BusInstance> _buses;
        private readonly object _syncLock = new object();
        private bool _disposed;

        public NamedBusFactory(IEnumerable<NamedBusOptions> busOptions, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _busOptions = busOptions?.ToDictionary(b => b.Name) ?? throw new ArgumentNullException(nameof(busOptions));
            _buses = new Dictionary<string, BusInstance>();
        }

        ~NamedBusFactory()
        {
            Dispose(false);
        }

        public IBus Get(string name) => ((NamedBusStarter)GetStarter(name)).Bus;

        public IBusStarter GetStarter(string name)
        {
            CheckIfDisposed();

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // ReSharper disable once InconsistentlySynchronizedField : justification - we check again
            // later inside a sync block. This is just to short-circuit.
            if (_buses.TryGetValue(name, out BusInstance busInstance))
            {
                return busInstance.BusStarter;
            }

            if (!_busOptions.TryGetValue(name, out NamedBusOptions busOptions))
            {
                throw new InvalidOperationException($"Bus with name '{name}' does not exist.");
            }

            lock (_syncLock)
            {
                // Try one more time to find bus, or otherwise create the instance.
                if (_buses.TryGetValue(name, out busInstance))
                {
                    return busInstance.BusStarter;
                }

                busInstance = CreateBusStarter(busOptions);
                _buses.Add(name, busInstance);
                return busInstance.BusStarter;
            }
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

        private void CheckIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Object is already disposed.");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                lock (_syncLock)
                {
                    foreach (BusInstance busInstance in _buses.Values)
                    {
                        busInstance.Dispose();
                    }
                }
            }

            _disposed = true;
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
