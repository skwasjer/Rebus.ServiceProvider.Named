using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Rebus.Bus;
using Xunit;

namespace Rebus.ServiceProvider.Named
{
    public abstract class DecoratedBusTests
    {
        private class FakeBusName { }

        private readonly FakeMessage _fakeMessage = new FakeMessage();
        private readonly IDictionary<string, string> _fakeHeaders = new Dictionary<string, string>();
        private readonly TimeSpan _fakeDeferTimeout = TimeSpan.FromMinutes(5);

        private readonly Mock<IBus> _decoratedBus;

        private IBus _sut;

        protected DecoratedBusTests()
        {
            _decoratedBus = new Mock<IBus>();
        }

        public class NamedBusTests : DecoratedBusTests
        {
            public NamedBusTests()
            {
                _sut = new NamedBus("", _decoratedBus.Object);
            }
        }

        public class TypedBusTests : DecoratedBusTests
        {
            public TypedBusTests()
            {
                _sut = new TypedBus<FakeBusName>(_decoratedBus.Object);
            }
        }

        [Fact]
        public void When_disposing_it_should_not_call_decorated()
        {
            // Act
            _sut.Dispose();

            // Assert
            _decoratedBus.Verify(m => m.Dispose(), Times.Never);
        }

        [Fact]
        public async Task When_sending_local_it_should_call_decorated()
        {
            // Act
            await _sut.SendLocal(_fakeMessage, _fakeHeaders);

            // Assert
            _decoratedBus.Verify(m => m.SendLocal(_fakeMessage, _fakeHeaders), Times.Once);
        }

        [Fact]
        public async Task When_sending_it_should_call_decorated()
        {
            // Act
            await _sut.Send(_fakeMessage, _fakeHeaders);

            // Assert
            _decoratedBus.Verify(m => m.Send(_fakeMessage, _fakeHeaders), Times.Once);
        }

        [Fact]
        public async Task When_deferring_local_it_should_call_decorated()
        {
            // Act
            await _sut.DeferLocal(_fakeDeferTimeout, _fakeMessage, _fakeHeaders);

            // Assert
            _decoratedBus.Verify(m => m.DeferLocal(_fakeDeferTimeout, _fakeMessage, _fakeHeaders), Times.Once);
        }

        [Fact]
        public async Task When_deferring_it_should_call_decorated()
        {
            // Act
            await _sut.Defer(_fakeDeferTimeout, _fakeMessage, _fakeHeaders);

            // Assert
            _decoratedBus.Verify(m => m.Defer(_fakeDeferTimeout, _fakeMessage, _fakeHeaders), Times.Once);
        }

        [Fact]
        public async Task When_replying_it_should_call_decorated()
        {
            // Act
            await _sut.Reply(_fakeMessage, _fakeHeaders);

            // Assert
            _decoratedBus.Verify(m => m.Reply(_fakeMessage, _fakeHeaders), Times.Once);
        }

        [Fact]
        public async Task When_subscribing_with_generic_it_should_call_decorated()
        {
            // Act
            await _sut.Subscribe<FakeMessage>();

            // Assert
            _decoratedBus.Verify(m => m.Subscribe<FakeMessage>(), Times.Once);
        }

        [Fact]
        public async Task When_subscribing_with_messageType_it_should_call_decorated()
        {
            // Act
            await _sut.Subscribe(typeof(FakeMessage));

            // Assert
            _decoratedBus.Verify(m => m.Subscribe(typeof(FakeMessage)), Times.Once);
        }

        [Fact]
        public async Task When_unsubscribing_with_generic_it_should_call_decorated()
        {
            // Act
            await _sut.Unsubscribe<FakeMessage>();

            // Assert
            _decoratedBus.Verify(m => m.Unsubscribe<FakeMessage>(), Times.Once);
        }

        [Fact]
        public async Task When_unsubscribing_with_messageType_it_should_call_decorated()
        {
            // Act
            await _sut.Unsubscribe(typeof(FakeMessage));

            // Assert
            _decoratedBus.Verify(m => m.Unsubscribe(typeof(FakeMessage)), Times.Once);
        }

        [Fact]
        public async Task When_publishing_it_should_call_decorated()
        {
            // Act
            await _sut.Publish(_fakeMessage, _fakeHeaders);

            // Assert
            _decoratedBus.Verify(m => m.Publish(_fakeMessage, _fakeHeaders), Times.Once);
        }

        [Fact]
        public void When_getting_advanced_it_should_return_same_instance()
        {
            _sut.Advanced.Should().BeSameAs(_decoratedBus.Object.Advanced);
        }
    }
}
