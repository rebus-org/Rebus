using System;
using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus
{
    class DisabledDataBus : IDataBus
    {
        public Task<DataBusAttachment> CreateAttachment(Stream source)
        {
            throw new InvalidOperationException(@"The data bus has not been enabled. Please configure the data bus with the .Options(...) configurer, e.g. like so:

Configure.With(..)
    .(...)
    .Options(o => {
        o.EnableDataBus()
            .StoreInSqlServer(....);
    })
    .(...)

in order to save data in a central SQL Server");
        }
    }
}