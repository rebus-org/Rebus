using System.Threading;
using System.Threading.Tasks;

namespace Rebus.Transport
{
    /// <summary>
    /// An optional extension that a transport can provide to allow for it to be interrogated
    /// </summary>
    public interface ITransportInspector
    {
        /// <summary>
        /// Gets the number of messages waiting in the queue
        /// </summary>
        Task<int> GetCount(CancellationToken cancellationToken);
    }
}