using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Rebus.DataBus;

namespace Rebus.Persistence.Throwing;

class DisabledDataBus : IDataBus
{
    public Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null) => throw GetException();

    public Task<Stream> OpenRead(string dataBusAttachmentId) => throw GetException();

    public Task<Dictionary<string, string>> GetMetadata(string dataBusAttachmentId) => throw GetException();

    public Task Delete(string dataBusAttachmentId) => throw GetException();

    public IEnumerable<string> Query(TimeRange readTime = null, TimeRange saveTime = null) => throw GetException();

    static InvalidOperationException GetException() => new(@"The data bus has not been enabled. Please configure the data bus with the .DataBus(...) configurer, e.g. like so:

Configure.With(..)
    .(...)
    .DataBus(d => d.StoreInFileSystem(""\\network-share\\some-path""))
    .Start();

to exchange attachments via a network share, or something like

Configure.With(..)
    .(...)
    .DataBus(d => d.StoreInSqlServer(....))
    .Start();

to use a central SQL Server.");
}