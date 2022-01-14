namespace Rebus.Encryption;

/// <summary>
/// Special headers that are used when the message contents are encrypted
/// </summary>
public static class EncryptionHeaders
{
    /// <summary>
    /// Optional header element that specifies an encryption algorithm that the contents have been encrypted with
    /// </summary>
    public const string ContentEncryption = "rbs2-content-encryption";

    /// <summary>
    /// Optional header element that specifies the key that the contents have been encrypted with
    /// </summary>
    public const string KeyId = "rbs2-encryption-keyid";
        
    /// <summary>
    /// When the contents have been encrypted, this header has the IV
    /// </summary>
    public const string ContentInitializationVector = "rbs2-encryption-iv";

    /// <summary>
    /// Special header that can be added to a message in order to disable encryption for that particular message
    /// </summary>
    public const string DisableEncryptionHeader = "rbs2-disable-encryption";
}