using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.Windsor;
using Rebus.Activation;
using Rebus.Handlers;

namespace Rebus.CastleWindsor
{
    public class CastleWindsorHandlerActivator : IHandlerActivator
    {
        readonly IWindsorContainer _windsorContainer;

        public CastleWindsorHandlerActivator(IWindsorContainer windsorContainer)
        {
            _windsorContainer = windsorContainer;
        }

        public async Task<IEnumerable<IHandleMessages<TMessage>>> GetHandlers<TMessage>(TMessage message)
        {
            return _windsorContainer.ResolveAll<IHandleMessages<TMessage>>();
        }
    }
}
