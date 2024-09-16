using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rebus.DataBus;

namespace Rebus.Persistence.Throwing;

sealed class DisabledDataBusStorageManagement : IDataBusStorageManagement
{
    public Task Delete(string id) => throw GetException();

    public IEnumerable<string> Query(TimeRange readTime = null, TimeRange saveTime = null) => throw GetException();

    static NotSupportedException GetException()
    {
        return new NotSupportedException(@"Sorry, but your chosen data bus storage did not provide the management service necessary to be able to do this.

Data bus storage implementors can choose to provide an implementation of IDataBusStorageManagement in addition to the usual IDataBusStorage service,
which will provide additional management-related capabilities to the data bus.");
    }
}