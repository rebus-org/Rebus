using System;
using Rebus.Configuration;
using Rebus.EventStore;
using Rebus.Logging;

namespace ExampleSend
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                    .Transport(t => t.UseEventStoreInOneWayClientMode())
                    .MessageOwnership(o => o.FromRebusConfigurationSection())
                    .CreateBus()
                    .Start();

                var timer = new System.Timers.Timer();
                timer.Elapsed += delegate { adapter.Bus.Send("karate er en livsstil"); };
                timer.Interval = 3000;
                timer.Start();
            }

            Console.WriteLine("Press ENTER to quit");
            Console.ReadLine();
        }
    }
}
