using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Rebus.Configuration;

namespace Rebus.Autofac
{
    public class AutofacContainerAdapter : IContainerAdapter
    {
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
            return container.Resolve<IEnumerable<IHandleMessages<T>>>().ToArray();
        }

        public void Release(IEnumerable handlerInstances)
        {
            foreach (var disposable in handlerInstances.OfType<IDisposable>())
            {
                disposable.Dispose();
            }
        }

        public void SaveBusInstances(IBus bus)
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance(bus).As<IBus>();
            builder.Register(a => MessageContext.GetCurrent()).InstancePerDependency();
            builder.Update(container);
        }
    }
}