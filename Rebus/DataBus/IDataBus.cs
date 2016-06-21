using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus
{
    /// <summary>
    /// API for Rebus' data bus
    /// </summary>
    public interface IDataBus
    {
        /// <summary>
        /// Creates an attachment from the given source stream, optionally providing some extra metadata to be stored along with the attachment
        /// </summary>
        Task<DataBusAttachment> CreateAttachment(Stream source, Dictionary<string, string> optionalMetadata = null);
    }
}