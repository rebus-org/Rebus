using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Handlers;

namespace Rebus.CastleWindsor
{
    /// <summary>
    /// Extension methods for making it easy to work with Rebus and Castle Windsor
    /// </summary>
    public static class WindsorContainerExtensions
    {
        /// <summary>
        /// Automatically picks up all handler types from the assembly containing <see cref="THandler"/> and registers them in the container
        /// </summary>
        public static IWindsorContainer AutoRegisterHandlersFromAssemblyOf<THandler>(this IWindsorContainer container) where THandler : IHandleMessages
        {
            return container
                .Register(
                    Classes.FromAssemblyContaining<THandler>()
                        .BasedOn<IHandleMessages>()
                        .WithServiceAllInterfaces()
                        .LifestyleTransient()
                        .Configure(c =>
                        {
                            c.Named(string.Format("{0} (auto-registered)", c.Implementation.FullName));
                        })
                );
        }
    }
}