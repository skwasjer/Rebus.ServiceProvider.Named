using Rebus.Bus;

namespace Rebus.ServiceProvider.Named
{
    internal class TypedBus<TName> : NamedBus, ITypedBus<TName>
        where TName : class
    {
        internal TypedBus(IBus bus)
            // Short circuit if named bus.
            : base(GetName(), bus)
        {
        }

        /// <summary>
        /// Returns the typed bus name.
        /// </summary>
        internal static string GetName() => typeof(TName).Name;
    }
}
