namespace Rebus.DataBus;

/// <summary>
/// Contains keys of standard metadata which is always available on data stored with the data bus
/// </summary>
public static class MetadataKeys
{
    /// <summary>
    /// Metadata key of the length in bytes of the stored data
    /// </summary>
    public const string Length = "Rbs2DataBusLength";

    /// <summary>
    /// Metadata key of the ISO8601-encoded time of when the data was stored
    /// </summary>
    public const string SaveTime = "Rbs2DataBusSaveTime";

    /// <summary>
    /// Metadata key of the ISO8601-encoding time of when the data was last read
    /// </summary>
    public const string ReadTime = "Rbs2DataBusReadTime";

    /// <summary>
    /// Optional header that might contain an encoding of the contents, e.g."gzip" for gzipped data.
    /// </summary>
    public const string ContentEncoding = "Rbs2ContentEncoding";
}