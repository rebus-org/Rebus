using System;
using System.Linq;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Messages;
using Rebus.Persistence.InMem;
using Rebus.Tests.Contracts.Timeouts;
using Rebus.Tests.Time;
using Rebus.Timeouts;

namespace Rebus.Tests.Persistence.InMem;

[TestFixture]
public class InMemoryBasicStoreAndRetrieveOperations : BasicStoreAndRetrieveOperations<InMemoryTimeoutManagerFactory> { }

public class InMemoryTimeoutManagerFactory : ITimeoutManagerFactory
{
    readonly FakeRebusTime _fakeRebusTime = new FakeRebusTime();
    readonly InMemoryTimeoutManager _timeoutManager;

    public InMemoryTimeoutManagerFactory()
    {
        _timeoutManager = new InMemoryTimeoutManager(_fakeRebusTime);
    }

    public ITimeoutManager Create()
    {
        return _timeoutManager;
    }

    public void Cleanup()
    {
        _fakeRebusTime.Reset();
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

    public void FakeIt(DateTimeOffset fakeTime)
    {
        _fakeRebusTime.FakeIt(fakeTime);
    }
}