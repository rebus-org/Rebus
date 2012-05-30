using System;
using System.Linq;
using StructureMap;

namespace Rebus.StructureMap
{
    public class StructureMapContainerAdapter : AbstractContainerAdapter
    {
        private readonly IContainer container;

        public StructureMapContainerAdapter(IContainer container)
        {
            this.container = container;
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            container.Configure(x =>
                {
                    foreach (var serviceType in serviceTypes)
                    {
                        x.For(serviceType).Singleton().Add(instance);
                    }
                });    
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            container.Configure(x =>
                {
                    // make registration with primary service type
                    var primaryServiceType = serviceTypes.First();
                    x.For(primaryServiceType).LifecycleIs(MapLifestyle(lifestyle)).Add(implementationType);

                    // forward resolution of additional service types to resolving the first
                    var secondaryServiceTypes = serviceTypes.Skip(1);
                    foreach (var serviceType in secondaryServiceTypes)
                    {
                        x.For(serviceType).Use(c => c.GetInstance(primaryServiceType));
                    }
                });
        }

        public override bool HasImplementationOf(Type serviceType)
        {
            return container.Model.HasImplementationsFor(serviceType);
        }

        public override T Resolve<T>()
        {
            return container.GetInstance<T>();
        }

        public override T[] ResolveAll<T>()
        {
            return container.GetAllInstances<T>().ToArray();
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