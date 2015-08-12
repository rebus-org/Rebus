using System;
using System.Collections;
using System.Collections.Generic;
using Ninject;
using Rebus.Configuration;
using System.Linq;

namespace Rebus.Ninject
{
    public class NinjectContainerAdapter : IContainerAdapter
    {
        readonly IKernel kernel;

        public NinjectContainerAdapter(IKernel kernel)
        {
            this.kernel = kernel;
        }
        
        public IEnumerable<IHandleMessages> GetHandlerInstancesFor<T>()
        {
            IEnumerable<IHandleMessages> handlers = kernel.GetAll<IHandleMessages<T>>();
            IEnumerable<IHandleMessages> asynchandlers = kernel.GetAll<IHandleMessagesAsync<T>>();
            return handlers.Union(asynchandlers).ToArray();
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
            kernel.Bind<IBus>().ToConstant(bus).InSingletonScope().Named("bus");
            kernel.Bind<IMessageContext>().ToMethod(k => MessageContext.GetCurrent()).InTransientScope().Named("messageContext");

            // We need to ensure that the kernel have resolved the interfaces once to make it control their lifetime
            kernel.Get<IBus>();
        }
    }
}
