using System;
using System.Linq;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.InMem
{
    public class InMemoryBasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<InMemoryTimeoutManagerFactory> { }

    public class InMemoryTimeoutManagerFactory : ITimeoutManagerFactory
    {
        readonly InMemoryTimeoutManager _timeoutManager = new InMemoryTimeoutManager();

        public ITimeoutManager Create()
        {
            return _timeoutManager;
        }

        public void Cleanup()
        {
        }

        public string GetDebugInfo()
        {
            return string.Join(Environment.NewLine, _timeoutManager
                .Select(deferredMessage =>
                {
                    var transportMessage = new TransportMessage(deferredMessage.Headers, deferredMessage.Body);
                    var label = transportMessage.GetMessageLabel();

                    return $"{deferredMessage.DueTime}: {label}";
                }));
        }
    }
}