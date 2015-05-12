using System;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Rebus.Handlers;

namespace Rebus.CastleWindsor
{
    /// <summary>
    /// Extension methods for making it easy to register Rebus handlers in your <see cref="WindsorContainer"/>
    /// </summary>
    public static class WindsorContainerExtensions
    {
        /// <summary>
        /// Automatically picks up all handler types from the calling assembly and registers them in the container
        /// </summary>
        public static IWindsorContainer AutoRegisterHandlersFromThisAssembly(this IWindsorContainer container)
        {
            if (container == null) throw new ArgumentNullException("container");

            var callingAssembly = Assembly.GetCallingAssembly();

            return RegisterAssembly(container, callingAssembly);
        }

        /// <summary>
        /// Automatically picks up all handler types from the assembly containing <see cref="THandler"/> and registers them in the container
        /// </summary>
        public static IWindsorContainer AutoRegisterHandlersFromAssemblyOf<THandler>(this IWindsorContainer container) where THandler : IHandleMessages
        {
            if (container == null) throw new ArgumentNullException("container");

            var assemblyToRegister = typeof(THandler).Assembly;
            
            return RegisterAssembly(container, assemblyToRegister);
        }

        /// <summary>
        /// Automatically picks up all handler types from the specified assembly and registers them in the container
        /// </summary>
        public static IWindsorContainer AutoRegisterHandlersFromAssembly(this IWindsorContainer container, Assembly assemblyToRegister)
        {
            if (container == null) throw new ArgumentNullException("container");
            if (assemblyToRegister == null) throw new ArgumentNullException("assemblyToRegister");

            return RegisterAssembly(container, assemblyToRegister);
        }

        static IWindsorContainer RegisterAssembly(IWindsorContainer container, Assembly assemblyToRegister)
        {
            return container
                .Register(
                    Classes.FromAssembly(assemblyToRegister)
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