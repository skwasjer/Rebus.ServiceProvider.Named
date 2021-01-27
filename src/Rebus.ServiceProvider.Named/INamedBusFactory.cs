using System;
using Rebus.Bus;
using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Factory to resolve a Rebus <see cref="IBus" /> by name.
    /// </summary>
    public interface INamedBusFactory
    {
        /// <summary>
        /// Resolves a Rebus bus by name.
        /// </summary>
        /// <param name="name">The bus name.</param>
        /// <returns>Returns a named bus instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the name is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no bus instance can be resolved.</exception>
        IBus Get(string name);

        /// <summary>
        /// Resolves a Rebus bus starter by name.
        /// </summary>
        /// <param name="name">The bus name.</param>
        /// <returns>Returns a named bus instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the name is <see langword="null" />.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no bus instance can be resolved.</exception>
        IBusStarter GetStarter(string name);
    }
}
