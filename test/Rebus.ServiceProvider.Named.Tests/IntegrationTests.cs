using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Persistence.InMem;
using Rebus.Pipeline;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;
using Xunit;
using Xunit.Abstractions;

namespace Rebus.ServiceProvider.Named
{
    public class IntegrationTests : IDisposable
    {
	    public class Bus2 { }

        public class MyMessage { }
        public class MyMessageProcessed { }

        private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;
        private readonly Mock<Action<string>> _callbackMock;

        public class Service1 : IHandleMessages<MyMessageProcessed>, IHandleMessages<MyMessage>
        {
            private readonly ITypedBus<Bus2> _bus2;
            private readonly INamedBus _bus1;
            private readonly Action<string> _callback;

            public Service1(INamedBusFactory busFactory, ITypedBus<Bus2> bus2, Action<string> messageCallback)
            {
                _bus1 = busFactory.Get("bus1");
                _bus2 = bus2;
                _callback = messageCallback;
            }

            public Task StartLongProcess()
            {
                // Sending a command to SQS.
                return _bus1.Send(new MyMessage());
            }

            public Task Handle(MyMessage message)
            {
                INamedBus thisBus = GetThisBus();
                _callback?.Invoke($"command handled by: {thisBus.Name}");

                // Received via bus1, but we're publishing event to Bus2.
                return _bus2.Publish(new MyMessageProcessed());
            }

            public Task Handle(MyMessageProcessed message)
            {
                INamedBus thisBus = GetThisBus();
                _callback?.Invoke($"event handled by: {thisBus.Name}");

                // Received through Bus2, message has been processed.
                return Task.CompletedTask;
            }

            private static INamedBus GetThisBus()
            {
                return (INamedBus)MessageContext.Current
                    .IncomingStepContext.Load<IServiceScope>()
                    .ServiceProvider
                    .GetRequiredService<IBus>();
            }
        }

        public IntegrationTests(ITestOutputHelper testOutputHelper)
        {
	        _callbackMock = new Mock<Action<string>>();

            _serviceProvider = new ServiceCollection()
                .AddSingleton(_callbackMock.Object)
                .AddTransient<Service1>()
                .AddRebusHandler<Service1>()

                .AddNamedRebus("bus1", c => c
	                .Logging(l => l.Use(new RebusTestLoggerFactory(testOutputHelper)))
	                .Options(o => o.LogPipeline())
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bus1-queue"))
                    .Routing(r => r.TypeBased().MapFallback("bus1-queue"))
                )
                .AddTypedRebus<Bus2>(c => c
	                .Logging(l => l.Use(new RebusTestLoggerFactory(testOutputHelper)))
	                .Options(o => o.LogPipeline())
                    .Transport(t => t.UseInMemoryTransport(new InMemNetwork(), "bus2-queue"))
                    .Subscriptions(s => s.StoreInMemory(new InMemorySubscriberStore()))
                )
                .BuildServiceProvider();
        }

        [Fact]
        public void Given_that_named_bus_is_registered_when_requesting_instance_it_should_not_throw()
        {
            Func<INamedBus> act = () => _serviceProvider.GetRequiredService<INamedBusFactory>().Get("bus1");

            INamedBus namedBus = act.Should()
                .NotThrow()
                .Which;
            namedBus.Should().NotBeNull();
            namedBus.Name.Should().Be("bus1");
        }

        [Fact]
        public void Given_that_typed_bus_is_registered_when_requesting_instance_it_should_not_throw()
        {
            Func<ITypedBus<Bus2>> act = () => _serviceProvider.GetRequiredService<ITypedBus<Bus2>>();

            INamedBus namedBus = act.Should()
                .NotThrow()
                .Which;
            namedBus.Should().NotBeNull();
            namedBus.Name.Should().Be(TypedBus<Bus2>.GetName());
        }

        [Fact]
        public async Task Given_that_a_message_is_sent_via_one_bus_when_handling_it_should_send_to_other_bus()
        {
            using var eventWasReceived = new ManualResetEvent(false);

            Service1 service = _serviceProvider.GetRequiredService<Service1>();

            int callbackCallCount = 0;
            var log = new List<string>();
            _callbackMock
                .Setup(m => m.Invoke(It.IsAny<string>()))
                .Callback<string>(s =>
                {
                    log.Add(s);
                    callbackCallCount++;
                    if (callbackCallCount >= 2)
                    {
                        eventWasReceived.Set();
                    }
                });

            var appBuilder = new ApplicationBuilder(_serviceProvider);
            appBuilder.UseNamedRebus("bus1");
            appBuilder.UseTypedRebus<Bus2>(bus => bus.Advanced.SyncBus.Subscribe(typeof(MyMessageProcessed)));

            // Act
            await service.StartLongProcess();

            // Assert
            eventWasReceived.WaitOne(TimeSpan.FromSeconds(30));
            log.Should()
                .BeEquivalentTo(new[]
                    {
                        "command handled by: bus1",
                        "event handled by: Bus2"
                    },
                    opts => opts.WithStrictOrdering());
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
