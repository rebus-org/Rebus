using System;
using System.Collections.Generic;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace Rebus.Castle.Windsor
{
    public class WindsorContainerAdapter : AbstractContainerAdapter
    {
        readonly IWindsorContainer container;

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

        public WindsorContainerAdapter(IWindsorContainer container)
        {
            this.container = container;

            container.Register(Component.For<InstanceDisposer>().LifeStyle.Singleton);

            container.Register(Component.For<IMessageContext>()
                                   .UsingFactoryMethod(k => MessageContext.GetCurrent())
                                   .LifeStyle.Transient);
        }

        public override void RegisterInstance(object instance, params Type[] serviceTypes)
        {
            container.Register(Component.For(serviceTypes).Instance(instance).NamedAutomatically(RandomName()));

            if (instance is IDisposable)
            {
                container.Resolve<InstanceDisposer>().AddInstance((IDisposable)instance);
            }
        }

        public override void Register(Type implementationType, Lifestyle lifestyle, params Type[] serviceTypes)
        {
            container.Register(Component.For(serviceTypes).ImplementedBy(implementationType)
                                   .LifeStyle.Is(MapLifestyle(lifestyle))
                                   .Named(RandomName()));
        }

        public override bool HasImplementationOf(Type serviceType)
        {
            return container.Kernel.HasComponent(serviceType);
        }

        public override T Resolve<T>()
        {
            return container.Resolve<T>();
        }

        public override T[] ResolveAll<T>()
        {
            return container.ResolveAll<T>();
        }

        public override void Release(object obj)
        {
            container.Release(obj);
        }

        static string RandomName()
        {
            return Guid.NewGuid().ToString();
        }

        LifestyleType MapLifestyle(Lifestyle lifestyle)
        {
            switch (lifestyle)
            {
                case Lifestyle.Singleton:
                    return LifestyleType.Singleton;
                case Lifestyle.Instance:
                    return LifestyleType.Transient;
                default:
                    throw new ArgumentOutOfRangeException("lifestyle");
            }
        }
    }
}
