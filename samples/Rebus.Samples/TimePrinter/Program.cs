using System;
using System.Timers;
using Rebus;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using Rebus.Logging;

namespace TimePrinter
{
    class Program
    {
        static void Main()
        {
            using (var adapter = new BuiltinContainerAdapter())
            using (var timer = new Timer())
            {
                adapter.Register(() => new PrintDateTime());

                var bus = Configure.With(adapter)
                                   .Logging(l => l.None())
                                   .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                                   .DetermineEndpoints(d => d.FromRebusConfigurationSection())
                                   .CreateBus()
                                   .Start();

                timer.Elapsed += delegate { bus.Send(DateTime.Now); };
                timer.Interval = 1000;
                timer.Start();

                Console.WriteLine("Press enter to quit");
                Console.ReadLine();
            }
        }
    }

    class PrintDateTime : IHandleMessages<DateTime>
    {
        public void Handle(DateTime currentDateTime)
        {
            Console.WriteLine("The time is {0}", currentDateTime);
        }
    }
}
