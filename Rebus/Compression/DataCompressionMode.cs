using Rebus.DataBus;

namespace Rebus.Compression;

/// <summary>
/// Enumerates strategies for when the data bus storage decorator GZIps the data
/// </summary>
public enum DataCompressionMode
{
    /// <summary>
    /// Always compresses data. Please note that this requires that data can be kept in memory as this
    /// is required by the streaming APIs used when compressing data
    /// </summary>
    Always,

    /// <summary>
    /// Compresses data when the <see cref="MetadataKeys.ContentEncoding"/> key is detected among the metadata
    /// of the save data and the value is "gzip"
    /// </summary>
    Explicit
}