using System;
using Ninject.Activation;
using Ninject.Infrastructure.Disposal;
using Ninject.Syntax;

namespace Rebus.Ninject
{
    public static class RequestScopeExtensionMethod
    {
        const string NinjectRebusMessageScope ="NinjectRebusMessageScope";

        public static IBindingNamedWithOrOnSyntax<T> InRebusMessageScope<T>(this IBindingInSyntax<T> syntax)
        {
            return syntax.InScope(GetScope);
        }

        static object GetScope(IContext arg)
        {
            if (!MessageContext.HasCurrent)
                throw new Exception("Rebus MessageContext has not been initialized!");

            var messageContext = MessageContext.GetCurrent();
            if (!messageContext.Items.ContainsKey(NinjectRebusMessageScope))
                messageContext.Items.Add(NinjectRebusMessageScope, new RebusMessageScope(messageContext));
            return messageContext.Items[NinjectRebusMessageScope];
        }
    }

    internal class RebusMessageScope : INotifyWhenDisposed
    {
        public RebusMessageScope(IMessageContext messageContext)
        {
            messageContext.Disposed += () => Disposed(this, new EventArgs());
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
        public event EventHandler Disposed;
    }
}