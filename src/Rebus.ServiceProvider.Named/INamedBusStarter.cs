using Rebus.Config;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Wraps a named bus, which has had its message processing stopped, by setting number of workers to 0.
    /// When <see cref="IBusStarter.Start"/> is called, workers are added, and message processing will start.
    /// </summary>
    public interface INamedBusStarter : IBusStarter
    {
    }
}
