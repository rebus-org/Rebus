namespace Rebus.Encryption;

/// <summary>
/// Interface to provide encryption/decryption custom implementation
/// </summary>
public interface IEncryptor
{
    /// <summary>
    /// Header name that will be added to an encrypted message
    /// </summary>
    string ContentEncryptionValue { get; }
        
    /// <summary>
    /// Decrypts the encrypted data
    /// </summary>
    byte[] Decrypt(EncryptedData encryptedData);
        
    /// <summary>
    /// Encrypts the given bytes and returns the encrypted data along with the salt in the returned <see cref="EncryptedData"/>
    /// </summary>
    EncryptedData Encrypt(byte[] bytes);
}