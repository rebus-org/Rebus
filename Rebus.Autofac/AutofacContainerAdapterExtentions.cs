using System;
using Autofac;
using Rebus.Pipeline;

namespace Rebus.Autofac
{
    public static class AutofacContainerAdapterExtentions
    {
        public static void RegisterRebus(this ContainerBuilder self)
        {
            self
                .Register(c =>
                {
                    var currentMessageContext = MessageContext.Current;
                    if (currentMessageContext == null)
                    {
                        throw new InvalidOperationException("Attempted to inject the current message context from MessageContext.Current, but it was null! Did you attempt to resolve IMessageContext from outside of a Rebus message handler?");
                    }
                    return currentMessageContext;
                })
                .InstancePerDependency()
                .ExternallyOwned();
        }
    }
}