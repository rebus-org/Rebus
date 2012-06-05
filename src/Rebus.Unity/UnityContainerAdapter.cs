using System;
using System.Collections.Generic;
using Microsoft.Practices.Unity;
using System.Linq;

namespace Rebus.Unity
{
    public class UnityContainerAdapter : AbstractContainerAdapter
    {
        readonly IUnityContainer unityContainer;
        readonly Dictionary<Type, string> defaultInstanceNames = new Dictionary<Type, string>(); 

        public UnityContainerAdapter(IUnityContainer unityContainer)
        {
            this.unityContainer = unityContainer;
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            foreach (var type in serviceTypes)
            {
                var name = Guid.NewGuid().ToString();
                PossiblyRegisterDefaultInstanceName(type, name);
                unityContainer.RegisterInstance(type, name, instance);
            }
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            var nameOfPrimary = Guid.NewGuid().ToString();
            var primaryServiceType = serviceTypes.First();
            PossiblyRegisterDefaultInstanceName(primaryServiceType, nameOfPrimary);
            unityContainer.RegisterType(primaryServiceType, implementationType, nameOfPrimary, MapLifestyle(lifestyle));

            foreach(var type in serviceTypes.Skip(1))
            {
                var name = Guid.NewGuid().ToString();
                PossiblyRegisterDefaultInstanceName(type, name);
                unityContainer.RegisterType(type, implementationType, name,
                                            MapLifestyle(lifestyle),
                                            new InjectionFactory(c => c.Resolve(primaryServiceType, nameOfPrimary)));
            }
        }

        void PossiblyRegisterDefaultInstanceName(Type type, string name)
        {
            if (defaultInstanceNames.ContainsKey(type)) return;
            defaultInstanceNames.Add(type, name);
        }

        static LifetimeManager MapLifestyle(Lifestyle lifestyle)
        {
            switch (lifestyle)
            {
                case Lifestyle.Singleton:
                    return new ContainerControlledLifetimeManager();
                case Lifestyle.Instance:
                    return new TransientLifetimeManager();
                default:
                    throw new ArgumentOutOfRangeException("lifestyle");
            }
        }

        public override bool HasImplementationOf(Type serviceType)
        {
            return defaultInstanceNames.ContainsKey(serviceType);
        }

        public override T Resolve<T>()
        {
            return unityContainer.Resolve<T>(defaultInstanceNames[typeof(T)]);
        }

        public override T[] ResolveAll<T>()
        {
            return unityContainer.ResolveAll<T>().ToArray();
        }

        public override void Release(object obj)
        {
            var disposable = obj as IDisposable;

            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
