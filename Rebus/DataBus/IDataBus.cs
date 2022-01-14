using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus;

/// <summary>
/// API for Rebus' data bus
/// </summary>
public interface IDataBus
{
    /// <summary>
    /// Creates an attachment from the given source stream, optionally providing some extra metadata to be stored along with the attachment
    /// </summary>
    Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null);

    /// <summary>
    /// Opens the attachment for reading, using the currently configured data bus
    /// </summary>
    Task<Stream> OpenRead(string dataBusAttachmentId);

    /// <summary>
    /// Uses the currently configured data bus to retrieve the metadata for the attachment with the given ID
    /// </summary>
    Task<Dictionary<string, string>> GetMetadata(string dataBusAttachmentId);

    /// <summary>
    /// Deletes the attachment with the given ID. Throws a <see cref="NotSupportedException"/>, if the underlying data bus storage does
    /// not provide this functionality.
    /// </summary>
    Task Delete(string dataBusAttachmentId);

    /// <summary>
    /// Iterates the attachments store and returns IDs of all attachments matching the given criteria
    /// </summary>
    IEnumerable<string> Query(TimeRange readTime = null, TimeRange saveTime = null);
}