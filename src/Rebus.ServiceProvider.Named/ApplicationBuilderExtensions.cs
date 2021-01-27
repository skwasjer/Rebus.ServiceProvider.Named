using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Extensions for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Starts the typed Rebus instance.
        /// </summary>
        /// <typeparam name="TName">The marker type name of the typed bus.</typeparam>
        /// <param name="applicationBuilder">The application builder.</param>
        /// <returns>The application builder to continue chaining configuring the request pipeline.</returns>
        public static IApplicationBuilder UseTypedRebus<TName>(this IApplicationBuilder applicationBuilder)
            where TName : class
        {
            return applicationBuilder.UseNamedRebus(TypedBus<TName>.GetName());
        }

        /// <summary>
        /// Starts the typed Rebus instance.
        /// </summary>
        /// <typeparam name="TName">The marker type name of the typed bus.</typeparam>
        /// <param name="applicationBuilder">The application builder.</param>
        /// <param name="configureBus">A delegate to configure the bus.</param>
        /// <returns>The application builder to continue chaining configuring the request pipeline.</returns>
        public static IApplicationBuilder UseTypedRebus<TName>(this IApplicationBuilder applicationBuilder, Action<IBus> configureBus)
            where TName : class
        {
            return applicationBuilder.UseNamedRebus(TypedBus<TName>.GetName(), configureBus);
        }

        /// <summary>
        /// Starts the named Rebus instance.
        /// </summary>
        /// <param name="applicationBuilder">The application builder.</param>
        /// <param name="name">The configured name of the bus.</param>
        /// <returns>The application builder to continue chaining configuring the request pipeline.</returns>
        public static IApplicationBuilder UseNamedRebus(this IApplicationBuilder applicationBuilder, string name)
        {
            return applicationBuilder.UseNamedRebus(name, null);
        }

        /// <summary>
        /// Starts the named Rebus instance.
        /// </summary>
        /// <param name="applicationBuilder">The application builder.</param>
        /// <param name="name">The configured name of the bus.</param>
        /// <param name="configureBus">A delegate to configure the bus.</param>
        /// <returns>The application builder to continue chaining configuring the request pipeline.</returns>
        public static IApplicationBuilder UseNamedRebus(this IApplicationBuilder applicationBuilder, string name, Action<IBus> configureBus)
        {
            if (applicationBuilder is null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            INamedBusFactory factory = applicationBuilder.ApplicationServices.GetRequiredService<INamedBusFactory>();
            IBusStarter busStarter = factory.GetStarter(name);
            busStarter.Start();

            configureBus?.Invoke(busStarter.Bus);
            return applicationBuilder;
        }
    }
}
