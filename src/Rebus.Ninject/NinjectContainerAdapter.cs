using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;

namespace Rebus.Ninject
{
    public class NinjectContainerAdapter : AbstractContainerAdapter
    {
        readonly IKernel kernel;

        class InstanceDisposer : IDisposable
        {
            readonly List<IDisposable> instances = new List<IDisposable>();

            public void AddInstance(IDisposable instanceToDisposeWhenDisposed)
            {
                instances.Add(instanceToDisposeWhenDisposed);
            }

            public void Dispose()
            {
                instances.ForEach(i => i.Dispose());
                instances.Clear();
            }
        }

        public NinjectContainerAdapter(IKernel kernel)
        {
            if (kernel == null)
                throw new ArgumentNullException("kernel");

            kernel.Bind<InstanceDisposer>().ToConstant(new InstanceDisposer()).InSingletonScope();

            kernel.Bind<IMessageContext>().ToMethod(k => MessageContext.GetCurrent()).InTransientScope();

            this.kernel = kernel;
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            kernel.Bind(serviceTypes).ToConstant(instance).Named(RandomName());

            if (instance is IDisposable)
            {
                kernel.Get<InstanceDisposer>().AddInstance((IDisposable)instance);
            }
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            var binding = kernel.Bind(serviceTypes).To(implementationType);
            switch(lifestyle)
            {
                case Lifestyle.Instance:
                    binding.InTransientScope().Named(RandomName());
                    break;
                case Lifestyle.Singleton:
                    binding.InSingletonScope().Named(RandomName());
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

            if (obj is IDisposable)
            {
                ((IDisposable) obj).Dispose();
            }
        }

        static string RandomName()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
