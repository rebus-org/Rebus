using System;
using System.Collections.Generic;
using Microsoft.Practices.Unity;
using System.Linq;
using Rebus.Logging;

namespace Rebus.Unity
{
    public class UnityContainerAdapter : AbstractContainerAdapter
    {
        static ILog logger;

        static UnityContainerAdapter()
        {
            RebusLoggerFactory.Changed += f => logger = f.GetCurrentClassLogger();
        }

        readonly IUnityContainer unityContainer;
        readonly Dictionary<Type, string> defaultInstanceNames = new Dictionary<Type, string>(); 

        public UnityContainerAdapter(IUnityContainer unityContainer)
        {
            this.unityContainer = unityContainer;
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                var name = Guid.NewGuid().ToString();
                PossiblyRegisterDefaultInstanceName(serviceType, name);

                logger.Debug("Registering instance of {0} as implementation of {1} named {2}", instance.GetType(), serviceType, name);

                unityContainer.RegisterInstance(serviceType, name, instance);

                // register a default as well
                unityContainer.RegisterInstance(serviceType, instance);
            }
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            var nameOfPrimary = Guid.NewGuid().ToString();
            var primaryServiceType = serviceTypes.First();
            PossiblyRegisterDefaultInstanceName(primaryServiceType, nameOfPrimary);

            logger.Debug("Registering type {0} as implementation of {1} named {2}", implementationType, primaryServiceType, nameOfPrimary);

            unityContainer.RegisterType(primaryServiceType, implementationType, nameOfPrimary, MapLifestyle(lifestyle));

            foreach(var additionalServiceType in serviceTypes.Skip(1))
            {
                var name = Guid.NewGuid().ToString();
                PossiblyRegisterDefaultInstanceName(additionalServiceType, name);
                
                logger.Debug("Registering type {0} as implementation of {1} named {2} with type forwarding to {3}/{4}", additionalServiceType, implementationType, name, primaryServiceType, nameOfPrimary);
                
                unityContainer.RegisterType(additionalServiceType, implementationType, name,
                                            MapLifestyle(lifestyle),
                                            new InjectionFactory(c => c.Resolve(primaryServiceType, nameOfPrimary)));
            }

            // and then, a bunch of defaults
            foreach (var serviceType in serviceTypes)
            {
                unityContainer.RegisterType(serviceType, implementationType,
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
