using System;
using System.Collections.Generic;
using Rebus;
using Rebus.Msmq;
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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Run()
        {
            var program = new Program();
            var msmqMessageQueue = new MsmqMessageQueue(@".\private$\sample.client", program)
                .PurgeInputQueue();
            var bus = new RebusBus(program, msmqMessageQueue, msmqMessageQueue, new InMemorySubscriptionStorage());
            bus.Start();

            do
            {
                Console.WriteLine("How many messages would you like to send?");
                var count = int.Parse(Console.ReadLine());

                for (var counter = 1; counter <= count; counter++)
                {
                    bus.Send(@".\private$\sample.server", new Ping { Message = string.Format("Msg. {0}", counter) });
                }

            } while (true);
        }

        public Type[] GetMessageTypes()
        {
            return typeof(Ping).Assembly.GetTypes();
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (typeof(T) == typeof(Pong))
            {
                return new[] {(IHandleMessages<T>) this};
            }

            return new IHandleMessages<T>[0];
        }

        public void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances)
        {
        }

        public void Handle(Pong message)
        {
            Console.WriteLine("Pong: {0}", message.Message);
        }
    }
}
