using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Wraps a named bus, which has had its message processing stopped, by setting number of workers to 0.
    /// When <see cref="IBusStarter.Start"/> is called, workers are added, and message processing will start.
    /// </summary>
    public interface INamedBusStarter : IBusStarter
    {
        /// <summary>
        /// Starts message processing and returns the bus instance
        /// </summary>
        new INamedBus Start();

        /// <summary>
        /// Gets the bus instance wrapped in this starter. The bus can be used to send, publish, subscribe, etc.
        /// </summary>
        new INamedBus Bus { get; }
    }
}
