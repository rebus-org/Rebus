using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.Windsor;
using Rebus.Activation;
using Rebus.Handlers;
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
            var handlerInstances = _windsorContainer.ResolveAll<IHandleMessages<TMessage>>();

            transactionContext.OnDisposed(() =>
            {
                foreach (var instance in handlerInstances)
                {
                    _windsorContainer.Release(instance);
                }
            });

            return handlerInstances;
        }
    }
}
