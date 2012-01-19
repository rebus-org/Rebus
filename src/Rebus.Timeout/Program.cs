using System;
using System.Collections;
using System.Collections.Generic;
using Rebus.Bus;
using Rebus.Log4Net;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Newtonsoft.JsonNET;
using Rebus.Transports.Msmq;
using Topshelf;
using log4net.Config;

namespace Rebus.Timeout
{
    class Program : IActivateHandlers
    {
        static TimeoutService timeoutService;

        static void Main()
        {
            XmlConfigurator.Configure();

            var msmqMessageQueue = new MsmqMessageQueue("rebus.timeout");
            var activator = new Program();

            RebusLoggerFactory.Current = new Log4NetLoggerFactory();
            var bus = new RebusBus(activator, msmqMessageQueue, msmqMessageQueue, null, null, null, new JsonMessageSerializer(), new TrivialPipelineInspector());

            timeoutService = new TimeoutService(bus);

            HostFactory
                .Run(s =>
                         {
                             const string text = "Rebus Timeout Service";

                             s.SetDescription("Rebus Timeout Service - Install named instance by adding '/instance:\"myInstance\"' when installing.");
                             s.SetDisplayName(text);
                             s.SetInstanceName("default");
                             s.SetServiceName("rebus_timeout_service");

                             const int numberOfWorkers = 1;

                             s.Service<TimeoutService>(c =>
                                                           {
                                                               c.ConstructUsing(() => timeoutService);
                                                               c.WhenStarted(t =>
                                                                                 {
                                                                                     bus.Start(numberOfWorkers);
                                                                                     t.Start();
                                                                                 });
                                                               c.WhenStopped(t =>
                                                                                 {
                                                                                     t.Stop();
                                                                                     bus.Dispose();
                                                                                 });
                                                           });
                         });
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (typeof(T) == typeof(RequestTimeoutMessage))
            {
                return new[] {(IHandleMessages<T>) timeoutService};
            }

            throw new InvalidOperationException(string.Format("Someone took the chance and sent a message of type {0} to me.", typeof(T)));
        }

        public IEnumerable<IMessageModule> GetMessageModules()
        {
            return new IMessageModule[0];
        }

        public void Release(IEnumerable handlerInstances)
        {
        }
    }
}
