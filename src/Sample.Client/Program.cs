using System;
using System.Collections.Generic;
using Rebus;
using Rebus.Persistence.InMemory;
using Rebus.Serialization.Json;
using Rebus.Transports.Msmq;
using Sample.Server.Messages;

namespace Sample.Client
{
    class Program : IActivateHandlers, IHandleMessages<Pong>, IDetermineDestination
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
            var msmqMessageQueue = new MsmqMessageQueue(@".\private$\sample.client", new JsonMessageSerializer())
                .PurgeInputQueue();
            var bus = new RebusBus(program, msmqMessageQueue, msmqMessageQueue, new InMemorySubscriptionStorage(), program);
            bus.Start();

            do
            {
                Console.WriteLine("How many messages would you like to send?");
                var count = int.Parse(Console.ReadLine());

                for (var counter = 1; counter <= count; counter++)
                {
                    bus.Send(new Ping { Message = string.Format("Msg. {0}", counter) });
                }

            } while (true);
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

        public string GetEndpointFor(Type messageType)
        {
            if (messageType == typeof(Ping))
            {
                return @".\private$\sample.server";
            }

            throw new ArgumentException(string.Format("Has no routing information for {0}", messageType));
        }
    }
}
