using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus;

class DefaultDataBus : IDataBus
{
    readonly IDataBusStorage _dataBusStorage;
    readonly IDataBusStorageManagement _dataBusStorageManagement;

    public DefaultDataBus(IDataBusStorage dataBusStorage, IDataBusStorageManagement dataBusStorageManagement)
    {
        _dataBusStorage = dataBusStorage ?? throw new ArgumentNullException(nameof(dataBusStorage));
        _dataBusStorageManagement = dataBusStorageManagement;
    }

    public async Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var id = Guid.NewGuid().ToString();

        await _dataBusStorage.Save(id, source, optionalMetadata);

        var attachment = new DataBusAttachment(id);

        return attachment;
    }

    public Task<Stream> OpenRead(string dataBusAttachmentId) => _dataBusStorage.Read(dataBusAttachmentId);

    public Task<Dictionary<string, string>> GetMetadata(string dataBusAttachmentId) => _dataBusStorage.ReadMetadata(dataBusAttachmentId);

    public Task Delete(string dataBusAttachmentId) =>  _dataBusStorageManagement.Delete(dataBusAttachmentId);

    public IEnumerable<string> Query(TimeRange readTime = null, TimeRange saveTime = null) => _dataBusStorageManagement.Query(readTime, saveTime);
}