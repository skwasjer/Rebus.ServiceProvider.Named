using Rebus.Bus;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Encapsulates a Rebus <see cref="IBus"/> that can be resolved by name.
    /// </summary>
    public interface INamedBus : IBus
    {
		/// <summary>
		/// Gets the bus name.
		/// </summary>
        string Name { get; }
    }
}
