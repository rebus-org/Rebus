using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebus.Configuration;
using Rebus.EventStore;
using Rebus.Logging;

namespace ExamplePublisher
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                         .Transport(t => t.UseEventStoreAndGetInputQueueNameFromAppConfig("ExamplePublisher"))
                         .CreateBus()
                         .Start();

                var startupTime = DateTime.Now;

                while (true)
                {
                    Console.WriteLine(@"a) Publish string
b) Publish DateTime
c) Publish TimeSpan
q) Quit");

                    var keyChar = char.ToLower(Console.ReadKey(true).KeyChar);

                    switch (keyChar)
                    {
                        case 'a':
                            adapter.Bus.Publish("Hello there, I'm a publisher!");
                            break;

                        case 'b':
                            adapter.Bus.Publish(DateTime.Now);
                            break;

                        case 'c':
                            adapter.Bus.Publish(DateTime.Now - startupTime);
                            break;

                        case 'q':
                            goto consideredHarmful;

                        default:
                            Console.WriteLine("There's no option ({0})", keyChar);
                            break;
                    }
                }

            consideredHarmful: ;
                Console.WriteLine("Quitting!");
            }
        }
    }
}
