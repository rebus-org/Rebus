using System;
using System.IO;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using Rebus.Logging;

namespace PubSub.Publisher
{
    class Program
    {
        static void Main()
        {
            using (var adapter = new BuiltinContainerAdapter())
            {
                Configure.With(adapter)
                    .Logging(l => l.ColoredConsole(minLevel: LogLevel.Warn))
                         .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                         .Subscriptions(s => s.StoreInXmlFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rebus_subscriptions.xml")))
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
