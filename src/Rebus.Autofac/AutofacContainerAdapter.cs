using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;

namespace Rebus.Autofac
{
    public class AutofacContainerAdapter : AbstractContainerAdapter
    {
        private readonly IContainer _container;

        public AutofacContainerAdapter(IContainer container)
        {
            if (container == null)
                throw new ArgumentNullException("container");
            _container = container;
        }

        public override bool HasImplementationOf(Type serviceType)
        {
            var result = _container.IsRegistered(serviceType); ;
            return result;
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            var builder = new ContainerBuilder();
            switch (lifestyle)
            {
                case Lifestyle.Instance:
                    builder.RegisterType(implementationType).InstancePerDependency().As(serviceTypes);
                    break;
                case Lifestyle.Singleton:
                default:
                    builder.RegisterType(implementationType).SingleInstance().As(serviceTypes);
                    break;
            }
            builder.Update(_container);
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            if (instance == null)
                return;

            var builder = new ContainerBuilder();
            builder.RegisterInstance(instance).As(serviceTypes);
            builder.Update(_container);
        }

        public override void Release(object obj)
        {
            // Autofac doesn't support releasing a specific object
            // Autofac will call Dispose on all IDisposable objects out of the box, when needed disposing
            // Code below forces object to be disposed
            var disposable = obj as IDisposable;
            if (disposable != null)
                disposable.Dispose();
        }

        public override T Resolve<T>()
        {
            return _container.Resolve<T>();
        }

        public override T[] ResolveAll<T>()
        {
            return _container.Resolve<IEnumerable<T>>().ToArray();
        }
    }
}
