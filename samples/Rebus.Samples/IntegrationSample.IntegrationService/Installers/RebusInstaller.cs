using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus;
using Rebus.Castle.Windsor;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using Rebus.Log4Net;

namespace IntegrationSample.IntegrationService.Installers
{
    public class RebusInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            Configure.With(new WindsorContainerAdapter(container))
                .Logging(l => l.Log4Net())
                .Transport(t => t.UseMsmqAndGetInputQueueNameFromAppConfig())
                .DetermineEndpoints(d => d.FromRebusConfigurationSection())
                .CreateBus().Start();
        }
    }
}