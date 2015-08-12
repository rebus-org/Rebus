using System;
using System.Collections.Generic;
using Autofac;
using Rebus.Autofac;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Testing;

namespace Rebus.Tests.Contracts.ContainerAdapters.Factories
{
    public class AutofacContainerAdapterFactory : IContainerAdapterFactory
    {
        List<IDisposable> disposables;
        IContainer container;
        TestMessageContext testMessageContext;

        public IContainerAdapter Create()
        {
            disposables = new List<IDisposable>();
            container = new ContainerBuilder().Build();
            disposables.Add(container);
            return new AutofacContainerAdapter(container);
        }

        public void DisposeInnerContainer()
        {
            container.Dispose();
        }

        public void StartUnitOfWork()
        {
            testMessageContext = new TestMessageContext();
            testMessageContext.Items["AutofacLifetimeScope"] = container.BeginLifetimeScope("UnitOfWorkLifetime");
            disposables.Add(TransactionContext.None());
            disposables.Add(FakeMessageContext.Establish(testMessageContext));
        }

        public void EndUnitOfWork()
        {
            var lifetimeScope = (ILifetimeScope) testMessageContext.Items["AutofacLifetimeScope"];
            lifetimeScope.Dispose();
        }

        class TestMessageContext : IMessageContext
        {
            public TestMessageContext()
            {
                Headers = new Dictionary<string, object>();
                Items = new Dictionary<string, object>();
            }

            public void Dispose()
            {
                Disposed();
            }

            public string ReturnAddress { get; private set; }
            public string RebusTransportMessageId { get; private set; }
            public IDictionary<string, object> Items { get; private set; }
            public void Abort()
            {
            }

            public event Action Disposed = delegate { };
            public object CurrentMessage { get; private set; }
            public IDictionary<string, object> Headers { get; private set; }
            public string StackTrace { get; private set; }
            public IReadOnlyCollection<Type> HandlersToSkip
            {
                get { throw new NotImplementedException(); }
            }
            public void SkipHandler(Type type)
            {
                throw new NotImplementedException();
            }
            public void DoNotSkipHandler(Type type)
            {
                throw new NotImplementedException();
            }
        }

        public void Register<TService, TImplementation>() where TService : class where TImplementation : TService
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<TImplementation>().As<TService>();
            containerBuilder.Update(container);
        }
    }
}