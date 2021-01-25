using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Transport;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Responsible for creating handlers for incoming messages from a named bus.
    /// </summary>
    internal class NamedBusHandlerActivator : IHandlerActivator
    {
        private readonly string _name;
        private readonly IHandlerActivator _handlerActivator;

        public NamedBusHandlerActivator(string name, IHandlerActivator handlerActivator)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _handlerActivator = handlerActivator ?? throw new ArgumentNullException(nameof(handlerActivator));
        }

        /// <inheritdoc />
        public Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            // Save the bus name in the step context so it can resolve the correct
            // bus instance when instantiating the handlers.
            IncomingStepContext stepContext = transactionContext.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);
            stepContext.Save(StepContextKeys.BusName, _name);
            return _handlerActivator.GetHandlers(message, transactionContext);
        }
    }
}
