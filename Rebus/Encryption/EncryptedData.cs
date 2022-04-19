using System;

namespace Rebus.Encryption;

/// <summary>
/// Represents a chunk of encrypted data along with the salt (a.k.a. "Initialization Vector"/"IV") that was used to encrypt it.
/// </summary>
public class EncryptedData
{
    /// <summary>
    /// Constructs an instance from the given bytes and iv.
    /// </summary>
    public EncryptedData(byte[] bytes, byte[] iv)
    {
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Iv = iv ?? throw new ArgumentNullException(nameof(iv));
    }

    /// <summary>
    /// Constructs an instance from the given bytes, iv and optional key id.
    /// </summary>
    public EncryptedData(byte[] bytes, byte[] iv, string keyId) : this(bytes, iv)
    {
        KeyId = keyId;
    }

    /// <summary>
    /// Gets the salt (a.k.a. "Initialization Vector"/"IV") from this encrypted data instance
    /// </summary>
    public byte[] Iv { get; }

    /// <summary>
    /// Gets the raw data from this encrypted data instance
    /// </summary>
    public byte[] Bytes { get; }

    /// <summary>
    /// Gets the identifier of the key used to encrypt this data instance
    /// </summary>
    public string KeyId { get; }
}