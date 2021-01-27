using Rebus.Bus;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Encapsulates a Rebus <see cref="IBus"/> that can be resolved by a type.
    /// </summary>
    /// <typeparam name="TName">A marker type to use for the bus name.</typeparam>
    // ReSharper disable once UnusedTypeParameter
    public interface ITypedBus<TName> : IBus
        where TName : class
    {
    }
}
