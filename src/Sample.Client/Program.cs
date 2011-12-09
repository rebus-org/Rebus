// Copyright 2011 Mogens Heller Grabe
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
using System;
using System.Collections.Generic;
using Rebus;
using Rebus.Bus;
using Rebus.Logging;
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
            RebusLoggerFactory.Current = new TraceLoggerFactory();

            var program = new Program();
            var msmqMessageQueue = new MsmqMessageQueue(@".\private$\sample.client").PurgeInputQueue();
            var inMemorySubscriptionStorage = new InMemorySubscriptionStorage();
            var jsonMessageSerializer = new JsonMessageSerializer();
            var sagaPersister = new InMemorySagaPersister();
            var inspectHandlerPipeline = new TrivialPipelineInspector();

            var bus = new RebusBus(program,
                                   msmqMessageQueue,
                                   msmqMessageQueue,
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

        public void ReleaseHandlerInstances(IEnumerable<IHandleMessages> handlerInstances)
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
