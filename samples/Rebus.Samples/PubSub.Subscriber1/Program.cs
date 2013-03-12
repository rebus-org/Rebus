using System;
using Rebus;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using Rebus.Logging;

namespace PubSub.Subscriber1
{
    class Program
    {
        static void Main()
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                adapter.Register(typeof(Handler));

                Configure.With(adapter)
                         .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                         .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                         .MessageOwnership(o => o.FromRebusConfigurationSection())
                         .CreateBus()
                         .Start();

                adapter.Bus.Subscribe<string>();
                adapter.Bus.Subscribe<DateTime>();
                adapter.Bus.Subscribe<TimeSpan>();

                Console.WriteLine("Press ENTER to quit");
                Console.ReadLine();
            }
        }
    }

    class Handler : IHandleMessages<string>, IHandleMessages<DateTime>, IHandleMessages<TimeSpan>
    {
        public void Handle(string message)
        {
            Console.WriteLine("Got string: {0}", message);
        }

        public void Handle(DateTime message)
        {
            Console.WriteLine("Got DateTime: {0}", message);
        }

        public void Handle(TimeSpan message)
        {
            Console.WriteLine("Got TimeSpan: {0}", message);
        }
    }
}
