using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;
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
        /// Uses an instance lifestyle where the instance is bound to (and thus will re-used across) the current Rebus transaction context
        /// </summary>
        public static ComponentRegistration<TService> PerRebusMessage<TService>(this LifestyleGroup<TService> lifestyleGroup) where TService : class
        {
            return lifestyleGroup.Registration.LifestylePerRebusMessage();
        }

        /// <summary>
        /// Uses an instance lifestyle where the instance is bound to (and thus will re-used across) the current Rebus transaction context
        /// </summary>
        public static ComponentRegistration<TService> LifestylePerRebusMessage<TService>(this ComponentRegistration<TService> registration) where TService : class
        {
            return registration.LifestyleScoped<RebusScopeAccessor>();
        }

        /// <summary>
        /// Automatically picks up all handler types from the calling assembly and registers them in the container
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
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