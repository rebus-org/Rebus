using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rebus.DataBus;

/// <summary>
/// Abstraction over the data bus storage
/// </summary>
public interface IDataBusStorage
{
    /// <summary>
    /// Saves the data from the given source stream under the given ID
    /// </summary>
    Task Save(string id, Stream source, Dictionary<string, string> metadata = null);

    /// <summary>
    /// Opens the data stored under the given ID for reading
    /// </summary>
    Task<Stream> Read(string id);

    /// <summary>
    /// Loads the metadata stored with the given ID
    /// </summary>
    Task<Dictionary<string, string>> ReadMetadata(string id);
}