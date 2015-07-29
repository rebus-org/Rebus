using System;
using System.Collections.Concurrent;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Lifestyle.Scoped;

namespace Rebus.Castle.Windsor
{
    [Serializable]
    public class PerTransportMessage : IScopeAccessor
    {
        readonly ConcurrentDictionary<IMessageContext, ILifetimeScope> scopes = new ConcurrentDictionary<IMessageContext, ILifetimeScope>();

        public ILifetimeScope GetScope(CreationContext context)
        {
            if (!MessageContext.HasCurrent)
                return null;

            return scopes.GetOrAdd(MessageContext.GetCurrent(), messageContext =>
            {
                var scope = new DefaultLifetimeScope(new ScopeCache());
                messageContext.Disposed += scope.Dispose;
                return scope;
            });
        }

        public void Dispose()
        {
            foreach (var scope in scopes.Values)
            {
                scope.Dispose();
            }
        }
    }
}