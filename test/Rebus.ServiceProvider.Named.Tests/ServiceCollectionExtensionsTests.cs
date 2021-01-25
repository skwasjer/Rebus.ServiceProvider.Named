using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Rebus.Bus;
using Rebus.Pipeline;
using Rebus.Transport;
using Xunit;

namespace Rebus.ServiceProvider.Named
{
    public class ServiceCollectionExtensionsTests
    {
        // ReSharper disable ClassNeverInstantiated.Local
        private class FakeTypedBusName1
        {
        }

        private class FakeTypedBusName2
        {
        }
        // ReSharper restore ClassNeverInstantiated.Local

        private const string BusName1 = "Bus1";
        private const string BusName2 = "Bus2";

        private readonly ServiceCollection _sut;

        public ServiceCollectionExtensionsTests()
        {
            _sut = new ServiceCollection();
        }

        [Theory]
        [MemberData(nameof(ExpectedServiceRegistrations))]
        public void When_adding_named_rebus_it_should_contain_expected_service(Action<IServiceCollection> addNamedRebus, ServiceDescriptor expectedServiceDescriptor)
        {
            // Act
            addNamedRebus(_sut);

            // Assert
            _sut.Should()
                .ContainEquivalentOf(
                    expectedServiceDescriptor,
                    opts => opts.Excluding(s => s.ImplementationFactory)
                );
        }

        [Fact]
        public void When_disposing_it_should_dispose_named_buses()
        {
            _sut.AddNamedRebus(BusName1, MemoryBusConfigurationHelper.ConfigureForInMem)
                .AddTypedRebus<FakeTypedBusName2>(MemoryBusConfigurationHelper.ConfigureForInMemWithSp);
            Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

            try
            {
                // Hydrate both buses.
                var bus1 = (NamedBus)serviceProvider.GetRequiredService<INamedBusFactory>().Get(BusName1);
                var bus2 = (NamedBus)serviceProvider.GetRequiredService<ITypedBus<FakeTypedBusName2>>();
                bus1.Advanced.Workers.SetNumberOfWorkers(1);
                bus2.Advanced.Workers.SetNumberOfWorkers(1);

                // Act
                serviceProvider.Dispose();

                // Assert
                bus1.Advanced.Workers.Count.Should().Be(0);
                bus2.Advanced.Workers.Count.Should().Be(0);
            }
            finally
            {
                // In case exception/assertion fails.
                serviceProvider.Dispose();
            }
        }

        [Fact]
        public void When_disposing_scope_it_should_not_dispose_bus_resolved_from_message_context()
        {
	        _sut.AddNamedRebus(BusName1, MemoryBusConfigurationHelper.ConfigureForInMem);
	        Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

	        var messageContext = new TestMessageContext(new object());
	        messageContext.IncomingStepContext.Save(StepContextKeys.BusName, BusName1);
	        AmbientTransactionContext.SetCurrent(messageContext.TransactionContext);

	        using IServiceScope serviceScope = serviceProvider.CreateScope();

			try
			{
		        // Hydrate bus.
		        var bus = (NamedBus)serviceScope.ServiceProvider.GetRequiredService<IBus>();
		        bus.Advanced.Workers.SetNumberOfWorkers(1);

		        // Act
		        serviceScope.Dispose();

		        // Assert
		        bus.Advanced.Workers.Count.Should().Be(1);
	        }
	        finally
	        {
		        // In case exception/assertion fails.
				serviceScope.Dispose();
		        serviceProvider.Dispose();
	        }
        }

		[Fact]
        public void Given_that_named_rebus_is_added_when_resolving_service_inside_of_message_context_it_should_resolve_expected_services()
        {
            _sut.AddNamedRebus(BusName1, MemoryBusConfigurationHelper.ConfigureForInMem);
            using Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

            var messageContext = new TestMessageContext(new object());
            messageContext.IncomingStepContext.Save(StepContextKeys.BusName, BusName1);
            AmbientTransactionContext.SetCurrent(messageContext.TransactionContext);

            try
            {
                // Act & assert
                serviceProvider.GetService<INamedBusFactory>().Should().BeOfType<NamedBusFactory>();
                serviceProvider.GetService<IEnumerable<INamedBus>>()
                    .Should()
                    .AllBeAssignableTo<NamedBus>()
                    .And.HaveCount(1);
                serviceProvider.GetService<IEnumerable<INamedBusStarter>>()
                    .Should()
                    .AllBeAssignableTo<NamedBusStarter>()
                    .And.HaveCount(1);
                serviceProvider.GetService<IBus>()
                    .Should()
                    .BeOfType<NamedBus>()
                    .Which.Name.Should()
                    .Be(BusName1);
                serviceProvider.GetService<IMessageContext>().Should().BeOfType<MessageContext>();
            }
            finally
            {
                AmbientTransactionContext.SetCurrent(null);
            }
        }

        [Fact]
        public void Given_that_named_rebus_is_added_when_resolving_service_outside_of_message_context_it_should_resolve_expected_services()
        {
            _sut.AddNamedRebus(BusName1, MemoryBusConfigurationHelper.ConfigureForInMem);
            using Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

            // Act & assert
            serviceProvider.GetService<INamedBusFactory>().Should().BeOfType<NamedBusFactory>();
            serviceProvider.GetService<IEnumerable<INamedBus>>()
                .Should()
                .AllBeAssignableTo<NamedBus>()
                .And.HaveCount(1);
            serviceProvider.GetService<IEnumerable<INamedBusStarter>>()
                .Should()
                .AllBeAssignableTo<NamedBusStarter>()
                .And.HaveCount(1);
            ((Action)(() => serviceProvider.GetService<IBus>())).Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Attempted to resolve IMessageContext outside of a Rebus handler*");
            ((Action)(() => serviceProvider.GetService<IMessageContext>())).Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Attempted to resolve IMessageContext outside of a Rebus handler*");
        }

        [Fact]
        public void Given_that_typed_rebus_is_added_when_resolving_service_inside_of_message_context_it_should_resolve_expected_services()
        {
            _sut.AddTypedRebus<FakeTypedBusName1>(MemoryBusConfigurationHelper.ConfigureForInMem);
            using Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

            var messageContext = new TestMessageContext(new object());
            messageContext.IncomingStepContext.Save(StepContextKeys.BusName, TypedBus<FakeTypedBusName1>.GetName());
            AmbientTransactionContext.SetCurrent(messageContext.TransactionContext);

            try
            {
                // Act & assert
                serviceProvider.GetService<INamedBusFactory>().Should().BeOfType<NamedBusFactory>();
                serviceProvider.GetService<IEnumerable<INamedBus>>()
                    .Should()
                    .AllBeAssignableTo<NamedBus>()
                    .And.HaveCount(1);
                serviceProvider.GetService<ITypedBus<FakeTypedBusName1>>().Should().BeOfType<TypedBus<FakeTypedBusName1>>();
                serviceProvider.GetService<IEnumerable<INamedBusStarter>>()
                    .Should()
                    .AllBeAssignableTo<NamedBusStarter>()
                    .And.HaveCount(1);
                serviceProvider.GetService<IBus>()
                    .Should()
                    .BeOfType<NamedBus>()
                    .Which.Name.Should()
                    .Be(TypedBus<FakeTypedBusName1>.GetName());
                serviceProvider.GetService<IMessageContext>().Should().BeOfType<MessageContext>();
            }
            finally
            {
                AmbientTransactionContext.SetCurrent(null);
            }
        }

        [Fact]
        public void Given_that_typed_rebus_is_added_when_resolving_service_outside_of_message_context_it_should_resolve_expected_services()
        {
            _sut.AddTypedRebus<FakeTypedBusName1>(MemoryBusConfigurationHelper.ConfigureForInMem);
            using Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

            // Act & assert
            serviceProvider.GetService<INamedBusFactory>().Should().BeOfType<NamedBusFactory>();
            serviceProvider.GetService<IEnumerable<INamedBus>>()
                .Should()
                .AllBeAssignableTo<NamedBus>()
                .And.HaveCount(1);
            serviceProvider.GetService<INamedBus>().Should().BeOfType<NamedBus>();
            serviceProvider.GetService<ITypedBus<FakeTypedBusName1>>().Should().BeOfType<TypedBus<FakeTypedBusName1>>();
            serviceProvider.GetService<IEnumerable<INamedBusStarter>>()
                .Should()
                .AllBeAssignableTo<NamedBusStarter>()
                .And.HaveCount(1);
            ((Action)(() => serviceProvider.GetService<IBus>())).Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Attempted to resolve IMessageContext outside of a Rebus handler*");
            ((Action)(() => serviceProvider.GetService<IMessageContext>())).Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Attempted to resolve IMessageContext outside of a Rebus handler*");
        }

        [Theory]
        [InlineData(BusName1)]
        [InlineData(BusName2)]
        [InlineData(nameof(FakeTypedBusName1))]
        [InlineData(nameof(FakeTypedBusName2))]
        public void Given_that_multiple_rebus_are_added_when_resolving_service_inside_of_message_context_it_should_resolve_expected_services(string busName)
        {
            _sut.AddTypedRebus<FakeTypedBusName1>(MemoryBusConfigurationHelper.ConfigureForInMemWithSp)
                .AddNamedRebus(BusName1, MemoryBusConfigurationHelper.ConfigureForInMem)
                .AddNamedRebus(BusName2, MemoryBusConfigurationHelper.ConfigureForInMemWithSp)
                .AddTypedRebus<FakeTypedBusName2>(MemoryBusConfigurationHelper.ConfigureForInMem);
            using Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

            var messageContext = new TestMessageContext(new object());
            messageContext.IncomingStepContext.Save(StepContextKeys.BusName, busName);
            AmbientTransactionContext.SetCurrent(messageContext.TransactionContext);

            try
            {
                // Act & assert
                serviceProvider.GetService<INamedBusFactory>().Should().BeOfType<NamedBusFactory>();
                serviceProvider.GetService<IEnumerable<INamedBus>>()
                    .Should()
                    .AllBeAssignableTo<NamedBus>()
                    .And.HaveCount(4);
                serviceProvider.GetService<ITypedBus<FakeTypedBusName1>>().Should().BeOfType<TypedBus<FakeTypedBusName1>>();
                serviceProvider.GetService<ITypedBus<FakeTypedBusName2>>().Should().BeOfType<TypedBus<FakeTypedBusName2>>();
                serviceProvider.GetService<IEnumerable<INamedBusStarter>>()
                    .Should()
                    .AllBeAssignableTo<NamedBusStarter>()
                    .And.HaveCount(4);
                serviceProvider.GetService<IBus>()
                    .Should()
                    .BeOfType<NamedBus>()
                    .Which.Name.Should()
                    .Be(busName);
                serviceProvider.GetService<IMessageContext>().Should().BeOfType<MessageContext>();
            }
            finally
            {
                AmbientTransactionContext.SetCurrent(null);
            }
        }

        [Fact]
        public void Given_that_multiple_rebus_are_added_when_resolving_service_outside_of_message_context_it_should_resolve_expected_services()
        {
            _sut.AddTypedRebus<FakeTypedBusName1>(MemoryBusConfigurationHelper.ConfigureForInMemWithSp)
                .AddNamedRebus(BusName1, MemoryBusConfigurationHelper.ConfigureForInMem)
                .AddNamedRebus(BusName2, MemoryBusConfigurationHelper.ConfigureForInMemWithSp)
                .AddTypedRebus<FakeTypedBusName2>(MemoryBusConfigurationHelper.ConfigureForInMem);
            using Microsoft.Extensions.DependencyInjection.ServiceProvider serviceProvider = _sut.BuildServiceProvider();

            // Act & assert
            serviceProvider.GetService<INamedBusFactory>().Should().BeOfType<NamedBusFactory>();
            serviceProvider.GetService<IEnumerable<INamedBus>>()
                .Should()
                .AllBeAssignableTo<NamedBus>()
                .And.HaveCount(4);
            serviceProvider.GetService<ITypedBus<FakeTypedBusName1>>().Should().BeOfType<TypedBus<FakeTypedBusName1>>();
            serviceProvider.GetService<ITypedBus<FakeTypedBusName2>>().Should().BeOfType<TypedBus<FakeTypedBusName2>>();
            serviceProvider.GetService<IEnumerable<INamedBusStarter>>()
                .Should()
                .AllBeAssignableTo<NamedBusStarter>()
                .And.HaveCount(4);
            ((Action)(() => serviceProvider.GetService<IBus>())).Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Attempted to resolve IMessageContext outside of a Rebus handler*");
            ((Action)(() => serviceProvider.GetService<IMessageContext>())).Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Attempted to resolve IMessageContext outside of a Rebus handler*");
        }

        [Theory]
        [MemberData(nameof(GetActs))]
        public void Given_name_is_already_used_when_adding_same_named_rebus_it_should_throw(Action<IServiceCollection> addNamedRebus)
        {
            addNamedRebus(_sut);

            // Act
            Action act = () => addNamedRebus(_sut);

            // Assert
            act.Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("The bus name '*' is already in use. Each bus must have a unique name.");
        }

        [Theory]
        [MemberData(nameof(GetActs))]
        public void Given_that_main_bus_is_registered_first_when_adding_named_rebus_it_should_throw(Action<IServiceCollection> addNamedRebus)
        {
            // Act
            Action act = () =>
            {
                _sut.AddRebus(c => c);
                addNamedRebus(_sut);
            };

            // Assert
            act.Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("A named or typed bus cannot be used in combination with a main bus.*");
        }

        [Theory]
        [MemberData(nameof(GetActs))]
        public void Given_that_named_bus_is_registered_first_when_adding_rebus_normally_afterwards_it_should_throw(Action<IServiceCollection> addNamedRebus)
        {
            // Act
            Action act = () =>
            {
                addNamedRebus(_sut);
                _sut.AddRebus(c => c);
            };

            // Assert
            act.Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage("Sorry, but it seems like Rebus has already been configured in this service collection*");
        }

        public static IEnumerable<object[]> GetActs()
        {
            Action<IServiceCollection>[] acts =
            {
                s => s.AddNamedRebus(BusName1, c => c),
                s => s.AddNamedRebus(BusName2, (c, _) => c),
                s => s.AddTypedRebus<FakeTypedBusName1>(c => c),
                s => s.AddTypedRebus<FakeTypedBusName2>((c, _) => c)
            };

            return acts.Select(act => new object[] { act });
        }

        public static IEnumerable<object[]> ExpectedServiceRegistrations()
        {
            Action<IServiceCollection>[] acts =
            {
                s => s.AddNamedRebus(BusName1, MemoryBusConfigurationHelper.ConfigureForInMem),
                s => s.AddNamedRebus(BusName2, MemoryBusConfigurationHelper.ConfigureForInMemWithSp),
                s => s.AddTypedRebus<FakeTypedBusName1>(MemoryBusConfigurationHelper.ConfigureForInMem),
                s => s.AddTypedRebus<FakeTypedBusName2>(MemoryBusConfigurationHelper.ConfigureForInMemWithSp)
            };

            for (int index = 0; index < acts.Length; index++)
            {
                Action<IServiceCollection> act = acts[index];

                switch (index)
                {
                    case 2:
                        yield return new object[] { act, ServiceDescriptor.Singleton(_ => Mock.Of<ITypedBus<FakeTypedBusName1>>()) };

                        break;
                    case 3:
                        yield return new object[] { act, ServiceDescriptor.Singleton(_ => Mock.Of<ITypedBus<FakeTypedBusName2>>()) };

                        break;
                    default:
                        yield return new object[] { act, ServiceDescriptor.Singleton<INamedBusFactory, NamedBusFactory>() };
                        yield return new object[] { act, ServiceDescriptor.Singleton(_ => Mock.Of<INamedBus>()) };
                        yield return new object[] { act, ServiceDescriptor.Singleton(_ => Mock.Of<INamedBusStarter>()) };
                        yield return new object[] { act, ServiceDescriptor.Scoped(_ => Mock.Of<IBus>()) };
                        yield return new object[] { act, ServiceDescriptor.Transient(_ => Mock.Of<IMessageContext>()) };

                        break;
                }
            }
        }
    }
}
