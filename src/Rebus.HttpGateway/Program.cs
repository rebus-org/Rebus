using Rebus.Log4Net;
using Rebus.Logging;
using Topshelf;
using log4net.Config;

namespace Rebus.HttpGateway
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
                    
                    s.UseLog4Net();
                    s.SetDescription("Rebus Gateway Service - Install named instance by adding '/instance:\"myInstance\"' when installing.");
                    s.SetDisplayName(text);
                    s.SetInstanceName("default");
                    s.SetServiceName("rebus_gateway_service");

                    s.Service<GatewayService>(c =>
                    {
                        c.ConstructUsing(GetGatewayServiceInstance);
                        c.WhenStarted(t => t.Start());
                        c.WhenStopped(t => t.Stop());
                    });
                });
        }

        static GatewayService GetGatewayServiceInstance()
        {
            var cfg = RebusGatewayConfigurationSection.LookItUp();

            var gateway = new GatewayService();

            if (cfg.Inbound != null)
            {
                gateway.ListenUri = cfg.Inbound.ListenUri;
                gateway.DestinationQueue = cfg.Inbound.DestinationQueue;
            }

            if (cfg.Outbound != null)
            {
                gateway.DestinationUri = cfg.Outbound.DestinationUri;
                gateway.ListenQueue = cfg.Outbound.ListenQueue;
            }

            return gateway;
        }
    }
}
