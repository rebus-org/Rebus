using System;
using System.Collections.Generic;
using Rebus;
using Rebus.Bus;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Transports.Msmq;
using Sample.Server.Messages;

namespace Sample.Server
{
    class Program : IActivateHandlers, IHandleMessages<Ping>, IDetermineDestination
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
            var msmqMessageQueue = new MsmqMessageQueue(@".\private$\sample.server", new JsonMessageSerializer())
                .PurgeInputQueue();
            var bus = new RebusBus(program, msmqMessageQueue, msmqMessageQueue, new InMemorySubscriptionStorage(), program);
            
            program.Bus = bus;

            bus.Start();

            Console.WriteLine("Server listening...");
            Console.ReadKey();
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

            Bus.Reply(new Pong { Message = message.Message });
        }

        public RebusBus Bus { get; set; }
        
        public string GetEndpointFor(Type messageType)
        {
            throw new NotImplementedException();
        }
    }
}
