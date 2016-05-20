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
        /// Creates an attachment from the given source stream
        /// </summary>
        Task<DataBusAttachment> CreateAttachment(Stream source);
    }
}