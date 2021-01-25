using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Moq;
using Rebus.Messages;
using Rebus.Pipeline;
using Rebus.TestHelpers;
using Rebus.Transport;

namespace Rebus.ServiceProvider.Named
{
    /// <summary>
    /// Since <see cref="FakeMessageContext"/> does not support incoming step context (for cancellation token support f.ex.) use this fake instead.
    /// </summary>
    public class TestMessageContext : IMessageContext
    {
        private ITransactionContext _txc;

        public TestMessageContext(object message)
            : this(
                new Message(new Dictionary<string, string>(), message),
                new TransportMessage(new Dictionary<string, string>(), Array.Empty<byte>())
            )
        {
        }

        public TestMessageContext(Message message, TransportMessage transportMessage)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            TransportMessage = transportMessage ?? throw new ArgumentNullException(nameof(transportMessage));
        }

        public ITransactionContext TransactionContext => AmbientTransactionContext.Current ?? (_txc ??= CreateTransactionContextMock());

        public IncomingStepContext IncomingStepContext
        {
            get => TransactionContext.GetOrNull<IncomingStepContext>(StepContext.StepContextKey);
        }

        public TransportMessage TransportMessage { get; }

        public Message Message { get; }

        public Dictionary<string, string> Headers => Message.Headers;

        private ITransactionContext CreateTransactionContextMock()
        {
            var items = new ConcurrentDictionary<string, object>();
            
            var mock = new Mock<ITransactionContext>();
            mock.Setup(m => m.Items).Returns(items);

            items.TryAdd(StepContext.StepContextKey, new IncomingStepContext(TransportMessage, mock.Object));

            return mock.Object;
        }
    }
}
