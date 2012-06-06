using Rebus.Log4Net;
using Rebus.Logging;
using Topshelf;
using log4net.Config;

namespace Rebus.Gateway
{
    class Program
    {
        static void Main()
        {
            XmlConfigurator.Configure();

            RebusLoggerFactory.Current = new Log4NetLoggerFactory();

            HostFactory
                .Run(s =>
                {
                    const string text = "Rebus Gateway Service";

                    s.SetDescription("Rebus Gateway Service - Install named instance by adding '/instance:\"myInstance\"' when installing.");
                    s.SetDisplayName(text);
                    s.SetInstanceName("default");
                    s.SetServiceName("rebus_gateway_service");

                    s.Service<GatewayService>(c =>
                    {
                        c.ConstructUsing(() => new GatewayService());
                        c.WhenStarted(t => t.Start());
                        c.WhenStopped(t => t.Stop());
                    });
                });
        }
    }
}
