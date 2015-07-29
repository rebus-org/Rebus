using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Rebus.Bus;
using Rebus.Configuration;

namespace Rebus.Autofac
{
    /// <summary>
    /// Implements an adapter for the Autofac IoC container, delegating instantiation
    /// of message handlers and their dependencies to Autofac. Components are resolved
    /// in a lifetime scope following the unit of work pattern in Rebus.
    /// </summary>
    public class AutofacContainerAdapter : IContainerAdapter
    {
        const string UnitOfWorkLifetime = "UnitOfWorkLifetime";
        const string ContextKey = "AutofacLifetimeScope";

        readonly IContainer container;

        /// <summary>
        /// Constructs an adapter for a specific Autofac container instance.
        /// </summary>
        /// <param name="container">The container from which message handlers are
        /// their dependencies will be resolved.</param>
        public AutofacContainerAdapter(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this.container = container;
        }

        /// <summary>
        /// Resolves a sequence of handlers from the container, where each handler
        /// implements the <see cref="IHandleMessages{T}"/> interface.
        /// </summary>
        /// <typeparam name="T">The type of message to get handlers for.</typeparam>
        /// <returns>Instances of handlers for the specified message type.</returns>
        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            var context = MessageContext.GetCurrent();
            var lifetimeScope = (ILifetimeScope)context.Items[ContextKey];

            IEnumerable<IHandleMessages> handlers = lifetimeScope.Resolve<IEnumerable<IHandleMessages<T>>>();
            IEnumerable<IHandleMessages> asyncHandlers = lifetimeScope.Resolve<IEnumerable<IHandleMessagesAsync<T>>>();
            return handlers.Union(asyncHandlers).ToArray();
        }

        /// <summary>
        /// This method is a no-op, since handlers will be released/disposed by
        /// Autofac at the end of their respective lifetimes.
        /// </summary>
        /// <param name="handlerInstances"></param>
        public void Release(IEnumerable handlerInstances)
        {
        }

        /// <summary>
        /// Registers the specified bus instance in the Autofac container, taking
        /// responsibility of its disposal when its the right time.
        /// </summary>
        /// <param name="bus">The hippie bus.</param>
        public void SaveBusInstances(IBus bus)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(bus).As<IBus>().SingleInstance();
            builder.Register(a => MessageContext.GetCurrent()).ExternallyOwned();
            builder.Update(container);

            bus.Advanced.Events.AddUnitOfWorkManager(new AutofacUnitOfWorkManager(container));
        }

        class AutofacUnitOfWorkManager : IUnitOfWorkManager
        {
            readonly IContainer container;

            public AutofacUnitOfWorkManager(IContainer container)
            {
                this.container = container;
            }

            public IUnitOfWork Create()
            {
                var context = MessageContext.GetCurrent();
                var lifetimeScope = container.BeginLifetimeScope(UnitOfWorkLifetime);
                context.Items.Add(ContextKey, lifetimeScope);

                return new AutofacUnitOfWork(lifetimeScope);
            }
        }

        class AutofacUnitOfWork : IUnitOfWork
        {
            readonly ILifetimeScope lifetimeScope;

            public AutofacUnitOfWork(ILifetimeScope lifetimeScope)
            {
                this.lifetimeScope = lifetimeScope;
            }

            public void Commit()
            {
            }

            public void Abort()
            {
            }

            public void Dispose()
            {
                lifetimeScope.Dispose();
            }
        }
    }
}