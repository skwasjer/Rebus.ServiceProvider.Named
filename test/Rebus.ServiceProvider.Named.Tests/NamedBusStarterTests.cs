using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using Rebus.Config;
using Xunit;

namespace Rebus.ServiceProvider.Named
{
    public class NamedBusStarterTests
    {
        private readonly Mock<IBusStarter> _originalBusStarterMock;
        private readonly Mock<INamedBus> _namedBusMock;
        private readonly NamedBusStarter _sut;

        public NamedBusStarterTests()
        {
            _originalBusStarterMock = new Mock<IBusStarter>();
            _namedBusMock = new Mock<INamedBus>();

            _sut = new NamedBusStarter(_originalBusStarterMock.Object, _namedBusMock.Object);
        }

        [Theory]
        [MemberData(nameof(CtorNullArgTestCases))]
        public void Given_null_arg_when_creating_instance_it_should_throw(IBusStarter originalBusStarter, INamedBus namedBus, string expectedParamName)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action act = () => new NamedBusStarter(originalBusStarter, namedBus);

            // Assert
            act.Should()
                .ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be(expectedParamName);
        }

        [Fact]
        public void When_accessing_bus_property_it_should_return_ctor_bus()
        {
            // Act & assert
            _sut.Bus.Should()
                .BeSameAs(_namedBusMock.Object)
                .And
                .BeSameAs(((IBusStarter)_sut).Bus);

            _originalBusStarterMock.Verify(m => m.Start(), Times.Never);
        }

        [Fact]
        public void When_starting_bus_it_should_return_start_ctor_bus_and_return_it()
        {
            // Act & assert
            INamedBus actual = _sut.Start();

            // Assert
            actual.Should().BeSameAs(_namedBusMock.Object);
            _originalBusStarterMock.Verify(m => m.Start(), Times.Once);
            actual.Should().BeSameAs(((IBusStarter)_sut).Start());
            _originalBusStarterMock.Verify(m => m.Start(), Times.Exactly(2));
        }

        public static IEnumerable<object[]> CtorNullArgTestCases()
        {
            IBusStarter originalBusStarter = Mock.Of<IBusStarter>();
            INamedBus namedBus = Mock.Of<INamedBus>();

            yield return new object[] { null, namedBus, nameof(originalBusStarter) };
            yield return new object[] { originalBusStarter, null, nameof(namedBus) };
        }
    }
}
