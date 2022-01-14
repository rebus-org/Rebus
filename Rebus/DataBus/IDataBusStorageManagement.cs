using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rebus.DataBus;

/// <summary>
/// Additional service which may/may not be provided by implementors of <see cref="IDataBusStorage"/>
/// </summary>
public interface IDataBusStorageManagement
{
    /// <summary>
    /// Deletes the attachment with the given ID
    /// </summary>
    Task Delete(string id);

    /// <summary>
    /// Iterates through IDs of attachments that match the given <paramref name="readTime"/> and <paramref name="saveTime"/> criteria.
    /// </summary>
    IEnumerable<string> Query(TimeRange readTime = null, TimeRange saveTime = null);
}