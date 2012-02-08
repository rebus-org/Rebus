using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.WindowsAzure;
using Rebus;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Newtonsoft.JsonNET;
using Rebus.Persistence.InMemory;
using Rebus.Transports.Azure.AzureMessageQueue;
using Rebus.Transports.Msmq;
using Sample.Server.Messages;

namespace Sample.Client
{
    class Program : IActivateHandlers, IHandleMessages<Pong>, IDetermineDestination
    {
        private const string clientQueue = "sample-client";
        private const string serverQueue = "sample-server";

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
            RebusLoggerFactory.Current = new TraceLoggerFactory();

            var program = new Program();
            var messageQueue = new MsmqMessageQueue(clientQueue).PurgeInputQueue();
            //var messageQueue = new AzureMessageQueue(CloudStorageAccount.DevelopmentStorageAccount, clientQueue, true);
            var inMemorySubscriptionStorage = new InMemorySubscriptionStorage();
            var jsonMessageSerializer = new JsonMessageSerializer();
            var sagaPersister = new InMemorySagaPersister();
            var inspectHandlerPipeline = new TrivialPipelineInspector();

            var bus = new RebusBus(program,
                                   messageQueue,
                                   messageQueue,
                                   inMemorySubscriptionStorage,
                                   sagaPersister,
                                   program, jsonMessageSerializer, inspectHandlerPipeline);
            
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

        public void Release(IEnumerable handlerInstances)
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
                return serverQueue;
            }

            throw new ArgumentException(string.Format("Has no routing information for {0}", messageType));
        }
    }
}
