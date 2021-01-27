using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rebus.Bus;
using Rebus.Pipeline;
using Xunit;

namespace Rebus.ServiceProvider.Named
{
    public class NamedBusFactoryTests : IDisposable
    {
        private readonly IReadOnlyList<NamedBusOptions> _namedBusOptions = new[]
        {
            new NamedBusOptions
            {
                Name = "bus1",
                ConfigureBus = MemoryBusConfigurationHelper.ConfigureForInMemWithSp
            },
            new NamedBusOptions
            {
                Name = "bus2",
                ConfigureBus = MemoryBusConfigurationHelper.ConfigureForInMemWithSp
            },
            new NamedBusOptions
            {
                Name = "bus3",
                ConfigureBus = MemoryBusConfigurationHelper.ConfigureForInMemWithSp
            }
        };

        private readonly FakeMessageHandler _messageHandler;
        private readonly IServiceProvider _serviceProvider;
        private readonly NamedBusFactory _sut;

        public NamedBusFactoryTests()
        {
            _messageHandler = new FakeMessageHandler();

            _serviceProvider = new ServiceCollection()
                .AddTransient(_ => MessageContext.Current)
                .AddRebusHandler(_ => _messageHandler)
                .BuildServiceProvider();

            _sut = new NamedBusFactory(_namedBusOptions, _serviceProvider);
        }

        [Theory]
        [MemberData(nameof(CtorNullArgTestCases))]
        public void Given_null_arg_when_creating_instance_it_should_throw(object busOptions, IServiceProvider serviceProvider, string expectedParamName)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action act = () => new NamedBusFactory((IEnumerable<NamedBusOptions>)busOptions, serviceProvider);

            // Assert
            act.Should()
                .ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be(expectedParamName);
        }

        [Theory]
        [InlineData("bus1")]
        [InlineData("bus2")]
        [InlineData("bus3")]
        public void Given_multiple_registered_buses_when_getting_by_name_it_should_return_expected(string busName)
        {
            // Act
            IBus actual = _sut.Get(busName);

            // Assert
            actual.Should()
                .NotBeNull()
                .And.BeOfType<NamedBus>()
                .Which.Name.Should()
                .Be(busName);
        }

        [Theory]
        [InlineData("unregistered-bus")]
        [InlineData("BUS1")]
        public void Given_that_bus_is_not_registered_when_getting_by_name_it_should_throw(string name)
        {
            // Act
            Action act = () => _sut.Get(name);

            // Assert
            act.Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Bus with name '*' does not exist.");
        }

        [Fact]
        public void When_getting_same_bus_by_name_twice_it_should_return_same_instance()
        {
            const string busName = "bus2";

            // Act
            IBus actual1 = _sut.Get(busName);
            IBus actual2 = _sut.Get(busName);

            // Assert
            actual1.Should().BeSameAs(actual2);
        }

        [Theory]
        [InlineData("bus1")]
        [InlineData("bus2")]
        [InlineData("bus3")]
        public async Task Given_multiple_registered_buses_when_sending_to_specific_bus_it_should_handle_message(string busName)
        {
            using var eventWasReceived = new ManualResetEvent(false);
            string handledByBusName = null;
            _messageHandler.Callback = bn =>
            {
                handledByBusName = bn;
                eventWasReceived.Set();
            };

            // Act
            INamedBusStarter actual = _sut.GetStarter(busName);
            IBus bus = actual.Start();

            await bus.SendLocal(new FakeMessage());

            // Assert
            eventWasReceived.WaitOne(TimeSpan.FromSeconds(5));
            handledByBusName.Should().Be(busName);
        }

        [Fact]
        public void Given_bus_name_is_null_when_resolving_it_should_throw()
        {
            string name = null;

            // Act
            // ReSharper disable once ExpressionIsAlwaysNull
            Action act = () => _sut.Get(name);

            // Assert
            act.Should()
                .ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be(nameof(name));
        }

        [Fact]
        public void When_disposing_it_should_dispose_each_bus_instance()
        {
            IBus bus1 = _sut.Get("bus1");
            IBus bus2 = _sut.Get("bus2");
            IBus bus3 = _sut.Get("bus3");
            bus1.Advanced.Workers.SetNumberOfWorkers(1);
            bus2.Advanced.Workers.SetNumberOfWorkers(0);
            bus3.Advanced.Workers.SetNumberOfWorkers(3);

            // Act
            _sut.Dispose();

            // Assert
            bus1.Advanced.Workers.Count.Should().Be(0);
            bus2.Advanced.Workers.Count.Should().Be(0);
            bus3.Advanced.Workers.Count.Should().Be(0);
        }

        public void Dispose()
        {
            (_serviceProvider as IDisposable)?.Dispose();
            _sut?.Dispose();
        }

        public static IEnumerable<object[]> CtorNullArgTestCases()
        {
            IEnumerable<NamedBusOptions> busOptions = Array.Empty<NamedBusOptions>();
            IServiceProvider serviceProvider = Mock.Of<IServiceProvider>();

            yield return new object[] { null, serviceProvider, nameof(busOptions) };
            yield return new object[] { busOptions, null, nameof(serviceProvider) };
        }
    }
}
