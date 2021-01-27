using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Rebus.Bus;
using Rebus.Bus.Advanced;

namespace Rebus.ServiceProvider.Named
{
    internal class NamedBus : IBus
    {
        internal NamedBus(string name, IBus bus)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            InnerBus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Dispose()
        {
            // Disposal is handled by factory.
        }

        public Task SendLocal(object commandMessage, IDictionary<string, string> optionalHeaders = null)
        {
            return InnerBus.SendLocal(commandMessage, optionalHeaders);
        }

        public Task Send(object commandMessage, IDictionary<string, string> optionalHeaders = null)
        {
            return InnerBus.Send(commandMessage, optionalHeaders);
        }

        public Task DeferLocal(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
        {
            return InnerBus.DeferLocal(delay, message, optionalHeaders);
        }

        public Task Defer(TimeSpan delay, object message, IDictionary<string, string> optionalHeaders = null)
        {
            return InnerBus.Defer(delay, message, optionalHeaders);
        }

        public Task Reply(object replyMessage, IDictionary<string, string> optionalHeaders = null)
        {
            return InnerBus.Reply(replyMessage, optionalHeaders);
        }

        public Task Subscribe<TEvent>()
        {
            return InnerBus.Subscribe<TEvent>();
        }

        public Task Subscribe(Type eventType)
        {
            return InnerBus.Subscribe(eventType);
        }

        public Task Unsubscribe<TEvent>()
        {
            return InnerBus.Unsubscribe<TEvent>();
        }

        public Task Unsubscribe(Type eventType)
        {
            return InnerBus.Unsubscribe(eventType);
        }

        public Task Publish(object eventMessage, IDictionary<string, string> optionalHeaders = null)
        {
            return InnerBus.Publish(eventMessage, optionalHeaders);
        }

        public IAdvancedApi Advanced => InnerBus.Advanced;

        public string Name { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal IBus InnerBus { get; }

        /// <inheritdoc />
        public override string ToString() => InnerBus.ToString();
    }
}
