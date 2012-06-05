using System;
using System.Timers;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus;
using Rebus.Castle.Windsor;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using Rebus.Logging;

namespace TimePrinter
{
    class Program
    {
        static WindsorContainer container;

        static void Main()
        {
            container = new WindsorContainer();

            container.Register(Component.For<IHandleMessages<DateTime>>().ImplementedBy<PrintDateTime>());

            var bus = Configure.With(new WindsorContainerAdapter(container))
                .Logging(l => l.None())
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                .DetermineEndpoints(d => d.FromRebusConfigurationSection())
                .CreateBus().Start();

            var timer = new Timer();
            timer.Elapsed += delegate { bus.Send(DateTime.Now); };
            timer.Interval = 1000;
            timer.Start();

            Console.WriteLine("Press enter to quit");
            Console.ReadLine();

            container.Dispose();
        }
    }

    public class PrintDateTime : IHandleMessages<DateTime>
    {
        public void Handle(DateTime currentDateTime)
        {
            Console.WriteLine("The time is {0}", currentDateTime);
        }
    }
}
