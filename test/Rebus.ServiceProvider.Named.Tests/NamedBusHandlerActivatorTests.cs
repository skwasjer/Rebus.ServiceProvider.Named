using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Rebus.Activation;
using Rebus.Handlers;
using Xunit;

namespace Rebus.ServiceProvider.Named
{
    public class NamedBusHandlerActivatorTests
    {
        private static readonly string BusName = "Bus-" + Guid.NewGuid();

        private readonly Mock<IHandlerActivator> _handlerActivatorMock;
        private readonly NamedBusHandlerActivator _sut;

        public NamedBusHandlerActivatorTests()
        {
            _handlerActivatorMock = new Mock<IHandlerActivator>();
            _sut = new NamedBusHandlerActivator(BusName, _handlerActivatorMock.Object);
        }

        [Theory]
        [MemberData(nameof(CtorNullArgTestCases))]
        public void Given_null_arg_when_creating_instance_it_should_throw(string name, IHandlerActivator handlerActivator, string expectedParamName)
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action act = () => new NamedBusHandlerActivator(name, handlerActivator);

            // Assert
            act.Should()
                .ThrowExactly<ArgumentNullException>()
                .Which.ParamName.Should()
                .Be(expectedParamName);
        }

        [Fact]
        public async Task Given_stepContext_has_busName_when_getting_handlers_it_should_set_busName_on_context_and_return_handlers()
        {
            var message = new object();
            var messageContext = new TestMessageContext(message);

            IHandleMessages<object>[] handlers = {
                Mock.Of<IHandleMessages<object>>(),
                Mock.Of<IHandleMessages<object>>()
            };

            _handlerActivatorMock
                .Setup(m => m.GetHandlers(message, messageContext.TransactionContext))
                .ReturnsAsync(handlers)
                .Verifiable();

            // Act
            IEnumerable<IHandleMessages<object>> actual = await _sut.GetHandlers(message, messageContext.TransactionContext);

            // Assert
            actual.Should().BeSameAs(handlers);
            messageContext.IncomingStepContext.Load<string>(StepContextKeys.BusName)
                .Should()
                .Be(BusName);
            _handlerActivatorMock.Verify();
        }

        public static IEnumerable<object[]> CtorNullArgTestCases()
        {
            string name = BusName;
            IHandlerActivator handlerActivator = Mock.Of<IHandlerActivator>();

            yield return new object[] { null, handlerActivator, nameof(name) };
            yield return new object[] { name, null, nameof(handlerActivator) };
        }
    }
}
