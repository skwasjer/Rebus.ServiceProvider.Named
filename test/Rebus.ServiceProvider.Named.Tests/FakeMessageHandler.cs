using System;
using System.Threading.Tasks;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Rebus.ServiceProvider.Named
{
    public class FakeMessageHandler : IHandleMessages<FakeMessage>
    {
        public Action<string> Callback { get; set; }

        public Task Handle(FakeMessage message)
        {
            string busName = MessageContext.Current?.IncomingStepContext.Load<string>(StepContextKeys.BusName);

            Callback?.Invoke(busName);

            return Task.CompletedTask;
        }
    }
}
