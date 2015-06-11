using System;
using System.Collections.Generic;
using System.Linq;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Tests.Contracts.Activation;
using SimpleInjector;

namespace Rebus.SimpleInjector.Tests
{
    public class SimpleInjectorContainerAdapterFactory : IContainerAdapterFactory
    {
        readonly Container _container = new Container();
        readonly HashSet<Type> _handlerTypesToRegister = new HashSet<Type>();
        readonly HashSet<IDisposable> _disposables = new HashSet<IDisposable>();

        public IHandlerActivator GetActivator()
        {
            _handlerTypesToRegister
                .SelectMany(type => GetHandlerInterfaces(type)
                    .Select(handlerType =>
                        new
                        {
                            HandlerType = handlerType,
                            ConcreteType = type
                        }))
                .GroupBy(a => a.HandlerType)
                .ForEach(a =>
                {
                    var serviceType = a.Key;

                    Console.WriteLine("Registering {0} => {1}", serviceType, string.Join(", ", a));
                    _container.RegisterAll(serviceType, a.Select(g => g.ConcreteType));
                });

            _handlerTypesToRegister.Clear();

            var containerAdapter = new SimpleInjectorContainerAdapter(_container);

            _disposables.Add(containerAdapter);

            return containerAdapter;
        }

        public void RegisterHandlerType<THandler>() where THandler : class, IHandleMessages
        {
            _handlerTypesToRegister.Add(typeof(THandler));
        }

        public void CleanUp()
        {
            _disposables.ForEach(d => d.Dispose());
            _disposables.Clear();
        }

        static IEnumerable<Type> GetHandlerInterfaces(Type handlerType)
        {
            return handlerType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHandleMessages<>));
        }
    }
}
