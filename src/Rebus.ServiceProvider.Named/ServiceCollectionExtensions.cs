using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Registration extensions to add a typed or named Rebus instance.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a typed Rebus instance. By using typed bus instances, you can host multiple Rebus instances and request a specific instance by requesting a <see cref="ITypedBus{TName}"/> from the service container.
        /// </summary>
        /// <remarks>Note that types must be unique.</remarks>
        /// <typeparam name="TName">The type that is used to uniquely identify the bus instance.</typeparam>
        /// <param name="services">The service collection in which to register the typed bus instance, and in which handlers will be registered.</param>
        /// <param name="configure">The Rebus configuration delegate.</param>
        /// <returns>The service collection to continue chaining service registrations.</returns>
        public static IServiceCollection AddTypedRebus<TName>(this IServiceCollection services, Func<RebusConfigurer, RebusConfigurer> configure)
            where TName : class
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return services.AddTypedRebus<TName>((configurer, provider) => configure(configurer));
        }

        /// <summary>
        /// Adds a typed Rebus instance. By using typed bus instances, you can host multiple Rebus instances and request a specific instance by requesting a <see cref="ITypedBus{TName}"/> from the service container.
        /// </summary>
        /// <remarks>Note that types must be unique.</remarks>
        /// <typeparam name="TName">The type that is used to uniquely identify the bus instance.</typeparam>
        /// <param name="services">The service collection in which to register the typed bus instance, and in which handlers will be registered.</param>
        /// <param name="configure">The Rebus configuration delegate.</param>
        /// <returns>The service collection to continue chaining service registrations.</returns>
        public static IServiceCollection AddTypedRebus<TName>(this IServiceCollection services, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
            where TName : class
        {
            string name = TypedBus<TName>.GetName();

            return services
                .AddNamedRebus(name, configure)
                // Register the typed bus.
                .AddSingleton<ITypedBus<TName>>(s =>
                    new TypedBus<TName>(s.GetRequiredService<INamedBusFactory>().Get(name))
                );
        }

        /// <summary>
        /// Adds a named Rebus instance. By using named bus instances, you can host multiple Rebus instances and request a specific instance by requesting the <see cref="INamedBusFactory"/> from the service container and then from the factory resolve the desired <see cref="IBus"/> by name.
        /// </summary>
        /// <remarks>Note that instance names must be unique.</remarks>
        /// <param name="services">The service collection in which to register the typed bus instance, and in which handlers will be registered.</param>
        /// <param name="name">The bus instance name.</param>
        /// <param name="configure">The Rebus configuration delegate.</param>
        /// <returns>The service collection to continue chaining service registrations.</returns>
        public static IServiceCollection AddNamedRebus(this IServiceCollection services, string name, Func<RebusConfigurer, RebusConfigurer> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            return services.AddNamedRebus(name, (configurer, provider) => configure(configurer));
        }

        /// <summary>
        /// Adds a named Rebus instance. By using named bus instances, you can host multiple Rebus instances and request a specific instance by requesting the <see cref="INamedBusFactory"/> from the service container and then from the factory resolve the desired <see cref="IBus"/> by name.
        /// </summary>
        /// <remarks>Note that instance names must be unique.</remarks>
        /// <param name="services">The service collection in which to register the typed bus instance, and in which handlers will be registered.</param>
        /// <param name="name">The bus instance name.</param>
        /// <param name="configure">The Rebus configuration delegate.</param>
        /// <returns>The service collection to continue chaining service registrations.</returns>
        public static IServiceCollection AddNamedRebus(this IServiceCollection services, string name, Func<RebusConfigurer, IServiceProvider, RebusConfigurer> configure)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Bus name cannot be empty.", nameof(name));
            }

            services
                // Initial check to ensure no bus is registered.
                .EnsureStandaloneIsNotRegistered()
                .EnsureUniqueName(name);

            // Register this specific named/typed bus replacements.
            services
                // TODO: consider options pattern.
                .AddSingleton(new NamedBusOptions
                {
                    ConfigureBus = configure,
                    Name = name
                });

            return TryAddSharedServices(services);
        }

        private static IServiceCollection TryAddSharedServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IHandlerActivator, DependencyInjectionHandlerActivator>();
            services.TryAddSingleton<INamedBusFactory, NamedBusFactory>();

            // In the outer provider, register IBus to resolve from the inner provider.
            // This means it will only work when in the context of a message handler
            // (ie.: a message context context and a service scope are both available.
            // Handlers can then still request the same IBus that the message was dispatched by
            // and do not need to request the by name or type.
            services.TryAddScoped<IBus>(s =>
            {
                IMessageContext messageContext = s.GetRequiredService<IMessageContext>(); // This will throw if not in message context.
                string busName = messageContext.IncomingStepContext.Load<string>(StepContextKeys.BusName);
                // Return a bus for this scope which does not dispose the actual bus
                // because it is a singleton and should not be disposed until the app terminates.
                return s.GetRequiredService<INamedBusFactory>().Get(busName);
            });

            services.TryAddTransient(s => MessageContext.Current ?? throw new InvalidOperationException(
                "Attempted to resolve IMessageContext outside of a Rebus handler, which is not possible. If you get this error, it's probably a sign that your service provider is being used outside of Rebus, where it's simply not possible to resolve a Rebus message context. Rebus' message context is only available to code executing inside a Rebus handler."));

            return services;
        }

        /// <summary>
        /// Throws when we detect AddRebus() has been used to register a 'single' bus.
        /// Named buses cannot be mixed.
        /// </summary>
        private static IServiceCollection EnsureStandaloneIsNotRegistered(this IServiceCollection services)
        {
            if (services.Any(descriptor => descriptor.ServiceType == typeof(IBus) && descriptor.Lifetime == ServiceLifetime.Singleton))
            {
                throw new InvalidOperationException(
                    $"A named or typed bus cannot be used in combination with a main bus. Replace the main bus registration (ie.: '.UseRebus()') with a named bus registration, and update usages of {nameof(IBus)} with {nameof(INamedBusFactory)} or {typeof(ITypedBus<>).Name}.");
            }

            return services;
        }

        /// <summary>
        /// Ensures the name is unique in the service container.
        /// </summary>
        private static IServiceCollection EnsureUniqueName(this IServiceCollection services, string name)
        {
            // Create/add or find the name tracker in the service collection.
            NameTracker nameTracker = (NameTracker)services
                .SingleOrDefault(descriptor => descriptor.ServiceType == typeof(NameTracker))
                ?.ImplementationInstance ?? new NameTracker();

            services.TryAddSingleton(nameTracker);

            // Not thread safe, but this should never called in non-thread safe manner anyway.
            if (nameTracker.Contains(name))
            {
                throw new InvalidOperationException($"The bus name '{name}' is already in use. Each bus must have a unique name.");
            }

            nameTracker.Add(name);
            return services;
        }

        /// <summary>
        /// Tracks usages of unique bus names.
        /// </summary>
        private class NameTracker : List<string>
        {
        }
    }
}
