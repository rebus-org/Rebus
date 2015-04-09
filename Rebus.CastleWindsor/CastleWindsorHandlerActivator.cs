using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Castle.Windsor;
using Rebus.Activation;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.CastleWindsor
{
    public class CastleWindsorHandlerActivator : IHandlerActivator
    {
        readonly IWindsorContainer _windsorContainer;

        public CastleWindsorHandlerActivator(IWindsorContainer windsorContainer)
        {
            _windsorContainer = windsorContainer;
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message, ITransactionContext transactionContext)
        {
            var handlerInstances = GetAllHandlerInstances<TMessage>();

            transactionContext.OnDisposed(() =>
            {
                foreach (var instance in handlerInstances)
                {
                    _windsorContainer.Release(instance);
                }
            });

            return handlerInstances;
        }

        IEnumerable<IHandleMessages<TMessage>> GetAllHandlerInstances<TMessage>()
        {
            var handledMessageTypes = typeof(TMessage).GetBaseTypes()
                .Concat(new[]{typeof(TMessage)});

            return handledMessageTypes
                .SelectMany(handledMessageType =>
                {
                    var implementedInterface = typeof (IHandleMessages<>).MakeGenericType(handledMessageType);

                    return _windsorContainer.ResolveAll(implementedInterface).Cast<IHandleMessages>();
                })
                .Cast<IHandleMessages<TMessage>>();
        }
    }
}
