using Castle.Windsor;
using Castle.Windsor.Installer;
using Topshelf;
using log4net.Config;

namespace IntegrationSample.IntegrationService
{
    class Program
    {
        IWindsorContainer container;

        static void Main()
        {
            XmlConfigurator.Configure();

            HostFactory
                .Run(c =>
                         {
                             c.SetServiceName(typeof(Program).FullName);
                             c.SetDescription("This is a simple integration service that demonstrates how communication with external web services can be made robust.");

                             c.Service<Program>(s =>
                                                    {
                                                        s.ConstructUsing(() => new Program());

                                                        s.WhenStarted(p => p.Start());
                                                        s.WhenStopped(p => p.Stop());
                                                    });
                             
                             c.DependsOnMsmq();
                             c.StartManually();
                         });
        }

        void Start()
        {
            container = new WindsorContainer().Install(FromAssembly.This());
        }

        void Stop()
        {
            container.Dispose();
        }
    }
}
