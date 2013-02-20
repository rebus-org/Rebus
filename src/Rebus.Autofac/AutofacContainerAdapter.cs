using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Rebus.Bus;
using Rebus.Configuration;

namespace Rebus.Autofac
{
    public class AutofacContainerAdapter : IContainerAdapter
    {
        const string ContextKey = "AutofacLifetimeScope";

        readonly IContainer container;

        public AutofacContainerAdapter(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            this.container = container;
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            var context = MessageContext.GetCurrent();
            var lifetimeScope = (ILifetimeScope)context.Items[ContextKey];

            return lifetimeScope.Resolve<IEnumerable<IHandleMessages<T>>>().ToArray();
        }

        public void Release(IEnumerable handlerInstances)
        {
        }

        public void SaveBusInstances(IBus bus)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(bus).As<IBus>();
            builder.Register(a => MessageContext.GetCurrent()).InstancePerDependency();
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
                var lifetimeScope = container.BeginLifetimeScope();
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