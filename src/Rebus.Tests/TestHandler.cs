using System;
using System.Threading.Tasks;

namespace Rebus.Tests
{
    internal class TestHandler<T> : IHandleMessages<T>
    {
        private readonly Action<T> action;

        public TestHandler(Action<T> action)
        {
            this.action = action;
        }

        public void Handle(T message)
        {
            action(message);
        }
    }

    internal class TestAsyncHandler<T> : IHandleMessagesAsync<T>
    {
        private readonly Func<T, Task> action;

        public TestAsyncHandler(Func<T, Task> action)
        {
            this.action = action;
        }

        public Task Handle(T message)
        {
            return action(message);
        }
    }
}