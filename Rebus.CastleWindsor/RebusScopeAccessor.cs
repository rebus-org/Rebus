using System;
using Castle.MicroKernel.Context;
using Castle.MicroKernel.Lifestyle.Scoped;
using Rebus.Pipeline;

namespace Rebus.CastleWindsor
{
    /// <summary>
    /// Castle Windsor scope accessor that gets a <see cref="DefaultLifetimeScope"/> which is stashed in the current transaction context
    /// </summary>
    class RebusScopeAccessor : IScopeAccessor
    {
        const string LifestimeScopeItemKey = "windsor-lifetime-scope";

        public ILifetimeScope GetScope(CreationContext context)
        {
            var messageContext = MessageContext.Current;

            if (messageContext == null)
            {
                throw new InvalidOperationException($"Attempted to resolve {context.RequestedType} outside of Rebus message context!");
            }

            var items = messageContext.TransactionContext.Items;

            object lifetimeScope;

            if (items.TryGetValue(LifestimeScopeItemKey, out lifetimeScope))
            {
                return (ILifetimeScope) lifetimeScope;
            }

            var defaultLifetimeScope = new DefaultLifetimeScope();

            items[LifestimeScopeItemKey] = defaultLifetimeScope;

            messageContext.TransactionContext.OnDisposed(() => defaultLifetimeScope.Dispose());

            return defaultLifetimeScope;
        }

        public void Dispose()
        {
            var messageContext = MessageContext.Current;
            if (messageContext == null) return;

            var items = messageContext.TransactionContext.Items;

            object lifetimeScope;

            if (!items.TryGetValue(LifestimeScopeItemKey, out lifetimeScope))
            {
                return;
            }

            ((ILifetimeScope) lifetimeScope).Dispose();
        }
    }
}