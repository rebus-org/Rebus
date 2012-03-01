using System;
using System.Linq;
using System.Collections.Generic;
using StructureMap;

namespace Rebus.StructureMap
{
    public class StructureMapContainerAdapter : AbstractContainerAdapter
    {
        private readonly IContainer _container;

        public StructureMapContainerAdapter(IContainer container)
        {
            _container = container;
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            _container.Configure(x =>
                {
                    foreach (var serviceType in serviceTypes)
                    {
                        x.For(serviceType).Add(instance);
                    }
                });    
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            _container.Configure(x =>
                {
                    foreach (var serviceType in serviceTypes)
                    {
                        x.For(serviceType)
                            .LifecycleIs(MapLifestyle(lifestyle))
                            .Add(implementationType);
                    }
                });
        }

        public override bool HasImplementationOf(Type serviceType)
        {
            return _container.Model.HasImplementationsFor(serviceType);
        }

        public override T Resolve<T>()
        {
            return _container.GetInstance<T>();
        }

        public override T[] ResolveAll<T>()
        {
            return _container.GetAllInstances<T>().ToArray();
        }

        public override void Release(object obj)
        {
            // StructureMap doesn't handle Dispose unless you use nested containers.
            // This is a manual override to work with the way Windsor does it. We should 
            // probably discuss if this is desired.
            var disposable = obj as IDisposable;

            if(disposable != null)
            {
                disposable.Dispose();
            }
        }

        private InstanceScope MapLifestyle(Lifestyle lifestyle)
        {
            switch (lifestyle)
            {
                case Lifestyle.Instance:
                    return InstanceScope.Transient;

                case Lifestyle.Singleton:
                    return InstanceScope.Singleton;

                default:
                    throw new ArgumentOutOfRangeException("lifestyle");
            }
        }
    }
}