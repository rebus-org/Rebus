using System;
using Rebus;
using Sample.Server.Messages;

namespace Sample.Client
{
    class Program : IProvideMessageTypes, IHandlerFactory, IHandleMessages<Pong>
    {
        static void Main()
        {
            try
            {
                Run();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Run()
        {
            var program = new Program();
            var bus = new RebusBus(new MsmqMessageQueue(@".\private$\sample.client", program), program);

            do
            {
                Console.WriteLine("Type a message to the server:");
                var message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message)) break;
                bus.Send(@".\private$\sample.server", new Ping {Message = message});
            } while (true);
        }

        public Type[] GetMessageTypes()
        {
            return typeof(Ping).Assembly.GetTypes();
        }

        public IHandleMessages<T> GetHandlerInstanceFor<T>()
        {
            try
            {
                return (IHandleMessages<T>) this;
            }
            catch(Exception e)
            {
                Console.WriteLine("Could not dispatch message of type {0}", typeof(T));
                return null;
            }
        }

        public void ReleaseHandlerInstance<T>(IHandleMessages<T> handlerInstance)
        {
        }

        public void Handle(Pong message)
        {
            Console.WriteLine("Pong: {0}", message.Message);
        }
    }
}
