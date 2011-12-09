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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Run()
        {
            RebusLoggerFactory.Current = new TraceLoggerFactory();

            var program = new Program();
            var msmqMessageQueue = new MsmqMessageQueue(@".\private$\sample.server");
            var inMemorySubscriptionStorage = new InMemorySubscriptionStorage();
            var jsonMessageSerializer = new JsonMessageSerializer();
            var sagaPersister = new InMemorySagaPersister();
            var inspectHandlerPipeline = new TrivialPipelineInspector();

            var bus = new RebusBus(program,
                                   msmqMessageQueue,
                                   msmqMessageQueue,
                                   inMemorySubscriptionStorage,
                                   sagaPersister,
                                   program, 
                                   jsonMessageSerializer, 
                                   inspectHandlerPipeline);

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

        public void ReleaseHandlerInstances(IEnumerable<IHandleMessages> handlerInstances)
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
