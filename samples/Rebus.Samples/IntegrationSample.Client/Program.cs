using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using IntegrationSample.Client.Handlers;
using IntegrationSample.IntegrationService.Messages;
using Rebus;
using Rebus.Castle.Windsor;
using Rebus.Transports.Msmq;
using Rebus.Configuration;
using Rebus.Logging;

namespace IntegrationSample.Client
{
    class Program
    {
        static void Main()
        {
            var container = new WindsorContainer()
                .Register(Component.For<IHandleMessages<GetGreetingReply>>()
                              .ImplementedBy<GetGreetingReplyHandler>()
                              .LifestyleTransient());

            var bus = Configure.With(new WindsorContainerAdapter(container))
                .Logging(l => l.None()) // disable logging to avoid polluting the console
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                .MessageOwnership(d => d.FromRebusConfigurationSection())
                .CreateBus().Start();

            Console.WriteLine("Press R to request a greeting and Q to quit...");

            var keepRunning = true;
            do
            {
                var key = Console.ReadKey(true);

                switch (char.ToLower(key.KeyChar))
                {
                    case 'r':
                        bus.Send(new GetGreetingRequest());
                        break;

                    case 'q':
                        keepRunning = false;
                        break;
                }
            } while (keepRunning);

            container.Dispose();
        }
    }
}
