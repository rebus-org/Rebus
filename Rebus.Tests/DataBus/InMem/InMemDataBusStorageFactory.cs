using System;
using Rebus.DataBus;
using Rebus.DataBus.InMem;
using Rebus.Tests.Contracts.DataBus;
using Rebus.Tests.Time;

namespace Rebus.Tests.DataBus.InMem;

public class InMemDataBusStorageFactory : IDataBusStorageFactory
{
    readonly InMemDataStore _inMemDataStore = new InMemDataStore();
    readonly FakeRebusTime _fakeRebusTime = new FakeRebusTime();

    public IDataBusStorage Create()
    {
        return new InMemDataBusStorage(_inMemDataStore, _fakeRebusTime);
    }

    public void CleanUp()
    {
        _fakeRebusTime.Reset();
    }

    public void FakeIt(DateTimeOffset fakeTime)
    {
        _fakeRebusTime.FakeIt(fakeTime);
    }
}