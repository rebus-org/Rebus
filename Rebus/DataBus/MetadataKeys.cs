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

    /// <summary>
    /// Optional header that might contain the name of which encryption type was used to encode the contents, e.g."aes" for AES encrypted data.
    /// </summary>
    public const string ContentEncryption = "Rbs2ContentEncryption";

    /// <summary>
    /// Optional header that contains the salt used when encrypting the data.
    /// </summary>
    public const string ContentInitializationVector = "Rbs2ContentInitializationVector";

    /// <summary>
    /// Optional header that indicates which key was used to encrypt the data.
    /// </summary>
    public const string ContentEncryptionKeyId = "Rbs2ContentEncryptionKeyId";
}