using System;
using System.Threading.Tasks;
using Rebus2.Activation;
using Rebus2.Dispatch;
using Rebus2.Messages;

namespace Rebus2.Pipeline.Receive
{
    public class DispatchStep : IStep
    {
        readonly Dispatcher _dispatcher;

        public DispatchStep(IHandlerActivator handlerActivator)
        {
            _dispatcher = new Dispatcher(handlerActivator);
        }

        public async Task Process(StepContext context, Func<Task> next)
        {
            var message = context.Load<Message>();
            
            await _dispatcher.Dispatch(message);
            
            await next();
        }
    }
}