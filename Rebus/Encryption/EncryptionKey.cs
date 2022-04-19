namespace Rebus.Encryption;

/// <summary>
/// Container of an encryptionkey and its identifier.
/// </summary>
public class EncryptionKey
{
    private readonly byte[] _key;
    private readonly string _identifier;

    /// <summary>
    /// A new encryptionkey with key content and identifier.
    /// </summary>
    /// <param name="key">Encryptionkey as byte array.</param>
    /// <param name="identifier">Identifier for provided key.</param>
    public EncryptionKey(byte[] key, string identifier)
    {
        _key = key;
        _identifier = identifier;
    }

    /// <summary>
    /// Key as byte array
    /// </summary>
    public byte[] Key => _key;

    /// <summary>
    /// Identifier for this key.
    /// Typically used for describing which key that was used for encrypting, and must be used for decrypting.
    /// </summary>
    public string Identifier => _identifier;
}