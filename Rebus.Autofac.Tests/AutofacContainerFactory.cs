using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Rebus.Activation;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;

namespace Rebus.Autofac.Tests
{
    public class AutofacContainerFactory : IContainerAdapterFactory
    {
        readonly ContainerBuilder _containerBuilder = new ContainerBuilder();
        readonly List<IDisposable> _disposables = new List<IDisposable>();

        public IHandlerActivator GetActivator()
        {
            var container = _containerBuilder.Build();
            _disposables.Add(container);
            return new AutofacContainerAdapter(container);
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _containerBuilder.RegisterType<THandler>()
                .As(GetHandlerInterfaces<THandler>().ToArray())
                .InstancePerDependency();
        }

        public void CleanUp()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();
        }

        static IEnumerable<Type> GetHandlerInterfaces<THandler>() where THandler : class, IHandleMessages
        {
            return typeof(THandler).GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
        }

    }
}