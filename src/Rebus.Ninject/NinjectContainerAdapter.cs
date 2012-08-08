using System;
using System.Linq;
using Ninject;

namespace Rebus.Ninject
{
    public class NinjectContainerAdapter : AbstractContainerAdapter
    {
        readonly IKernel kernel;

        public NinjectContainerAdapter(IKernel kernel)
        {
            if (kernel == null)
                throw new ArgumentNullException("kernel");

            kernel.Bind<IMessageContext>().ToMethod(k => MessageContext.GetCurrent()).InTransientScope();

            this.kernel = kernel;
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            kernel.Bind(serviceTypes).ToConstant(instance);
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            var binding = kernel.Bind(serviceTypes).To(implementationType);
            switch(lifestyle)
            {
                case Lifestyle.Instance:
                    //nothing
                    break;
                case Lifestyle.Singleton:
                    binding.InSingletonScope();
                    break;
                default:
                    throw new ArgumentException(string.Format("{0} does not support the lifestyle: {1}",
                        GetType().Name,
                        lifestyle.ToString()));
                    break;
            }
        }

        public override bool HasImplementationOf(Type serviceType)
        {
            return kernel.GetBindings(serviceType).Any();
        }

        public override T Resolve<T>()
        {
            return kernel.Get<T>();
        }

        public override T[] ResolveAll<T>()
        {
            return kernel.GetAll<T>().ToArray();
        }

        public override void Release(object obj)
        {
            kernel.Release(obj);
        }
    }
}
