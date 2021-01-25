using System;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Rebus.ServiceProvider.Named
{
    public class ApplicationBuilderExtensionsTests : IDisposable
    {
        private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;
        private readonly IApplicationBuilder _sut;

        public ApplicationBuilderExtensionsTests()
        {
            _serviceProvider = new ServiceCollection()
                .AddTypedRebus<Bus1>(MemoryBusConfigurationHelper.ConfigureForInMem)
                .AddNamedRebus("bus2", MemoryBusConfigurationHelper.ConfigureForInMem)
                .BuildServiceProvider();

            _sut = new ApplicationBuilder(_serviceProvider);
        }

        [Fact]
        public void Given_that_typed_bus_is_not_registered_when_using_typed_bus_it_should_throw()
        {
            // Act
            Action act = () => _sut.UseTypedRebus<UnregisteredBus>();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Bus with name '*' does not exist.");
        }

        [Fact]
        public void Given_that_named_bus_is_not_registered_when_using_named_bus_it_should_throw()
        {
            // Act
            Action act = () => _sut.UseNamedRebus("unregistered-bus");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Bus with name '*' does not exist.");
        }

        [Fact]
        public void Given_that_typed_bus_is_registered_when_using_typed_bus_it_should_not_throw_and_start()
        {
            // Act
            Action act = () => _sut.UseTypedRebus<Bus1>();

            // Assert
            act.Should().NotThrow();
            _serviceProvider.GetRequiredService<ITypedBus<Bus1>>()
                .Advanced.Workers.Count.Should()
                .BeGreaterThan(0);
        }

        [Fact]
        public void Given_that_named_bus_is_registered_when_using_typed_bus_it_should_not_throw_and_start()
        {
            const string busName = "bus2";

            // Act
            Action act = () => _sut.UseNamedRebus(busName);

            // Assert
            act.Should().NotThrow();
            _serviceProvider.GetRequiredService<INamedBusFactory>()
                .Get(busName)
                .Advanced.Workers.Count.Should()
                .BeGreaterThan(0);
        }

        private class Bus1 { }
        private class UnregisteredBus { }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
