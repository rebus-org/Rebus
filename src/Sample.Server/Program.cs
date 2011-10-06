using System;
using System.Collections.Generic;
using Rebus;
using Rebus.Msmq;
using Sample.Server.Messages;

namespace Sample.Server
{
    class Program : IProvideMessageTypes, IHandlerFactory, IHandleMessages<Ping>
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
            var msmqMessageQueue = new MsmqMessageQueue(@".\private$\sample.server", program)
                .PurgeInputQueue();
            var bus = new RebusBus(program, msmqMessageQueue, msmqMessageQueue, new InMemorySubscriptionStorage());
            
            program.Bus = bus;

            bus.Start();

            Console.WriteLine("Server listening...");
            Console.ReadKey();
        }

        public Type[] GetMessageTypes()
        {
            return typeof (Ping).Assembly.GetTypes();
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (typeof(T) == typeof(Ping))
            {
                return new[] { (IHandleMessages<T>)this };
            }

            return new IHandleMessages<T>[0];
        }

        public void ReleaseHandlerInstances<T>(IEnumerable<IHandleMessages<T>> handlerInstances)
        {
        }

        public void Handle(Ping message)
        {
            Console.WriteLine("Got {0}", message.Message);

            Bus.Send(@".\private$\sample.client", new Pong { Message = message.Message });
        }

        public RebusBus Bus { get; set; }
    }
}
