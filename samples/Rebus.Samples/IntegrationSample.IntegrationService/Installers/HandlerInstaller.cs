using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus;

namespace IntegrationSample.IntegrationService.Installers
{
    public class HandlerInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(AllTypes.FromThisAssembly().BasedOn<IHandleMessages>()
                                   .WithService.AllInterfaces()
                                   .LifestyleTransient());
        }
    }
}