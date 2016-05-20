namespace Rebus.Encryption
{
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
        /// Decryption method
        /// </summary>
        /// <param name="encryptedData"></param>
        /// <returns>Decrypted content</returns>
        byte[] Decrypt(EncryptedData encryptedData);
        /// <summary>
        /// Encryption method
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns>Encrypted data and IV used for encryption. See <see cref="EncryptedData"/> for details</returns>
        EncryptedData Encrypt(byte[] bytes);
    }
}