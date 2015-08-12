using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rebus.Configuration;
using SimpleInjector;

namespace Rebus.SimpleInjector
{
    public class SimpleInjectorAdapter : IContainerAdapter
    {
        readonly Container container;

        public Container Container
        {
            get { return container; }
        }

        public SimpleInjectorAdapter(Container container)
        {
            if (container == null)
                throw new ArgumentNullException("container");
            this.container = container;
        }

        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            IEnumerable<IHandleMessages> handlers = container.GetAllInstances(typeof(IHandleMessages<T>)).Cast<IHandleMessages<T>>();
            IEnumerable<IHandleMessages> asyncHandlers =  container.GetAllInstances(typeof (IHandleMessagesAsync<T>)).Cast<IHandleMessagesAsync<T>>();

            return handlers.Union(asyncHandlers).ToArray();
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
            container.Register<IBus>(() => bus,Lifestyle.Singleton);

            container.Register<IMessageContext>(()=>MessageContext.GetCurrent(),Lifestyle.Transient);
        }
    }
}
