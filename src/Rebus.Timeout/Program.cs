using Topshelf;
using log4net.Config;

namespace Rebus.Timeout
{
    class Program
    {
        static void Main()
        {
            XmlConfigurator.Configure();

            HostFactory
                .Run(s =>
                         {
                             const string text = "Rebus Timeout Service";

                             s.SetDescription("Rebus Timeout Service - Install named instance by adding '/instance:\"myInstance\"' when installing.");
                             s.SetDisplayName(text);
                             s.SetInstanceName("default");
                             s.SetServiceName("rebus_timeout_service");

                             s.Service<TimeoutService>(c =>
                                                           {
                                                               c.ConstructUsing(() => new TimeoutService(new InMemoryTimeoutStorage()));
                                                               c.WhenStarted(t => t.Start());
                                                               c.WhenStopped(t => t.Stop());
                                                           });
                         });
        }
    }
}
