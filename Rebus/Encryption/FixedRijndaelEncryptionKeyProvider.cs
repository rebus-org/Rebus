using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Rebus.Encryption;

/// <summary>
/// Helps with providing encryption keys for encrypting and decrypting messages when the key is fixed.
/// </summary>
public class FixedRijndaelEncryptionKeyProvider : IEncryptionKeyProvider
{
    const string KeyIdentifier = "default";
    readonly Task<EncryptionKey> _encryptionKey;

    /// <summary>
    /// Creates the keyprovider with the specified key - the key must be a valid, base64-encoded key.
    /// </summary>
    public FixedRijndaelEncryptionKeyProvider(string encryptionKey)
    {
        try
        {
            var key = new EncryptionKey(Convert.FromBase64String(encryptionKey), KeyIdentifier);

            // verify the key - will throw if it's not a valid size/padded correctly
            using var rijndael = new RijndaelManaged();
            rijndael.Key = key.Key;

            _encryptionKey = Task.FromResult(key);
        }
        catch (Exception exception)
        {
            throw new ArgumentException(
                $@"Could not initialize the encryption algorithm with the specified key (not shown here for security reasons) - if you're unsure how to get a valid key, here's a newly generated key that you can use:

    {GenerateNewKey()}

I promise that the suggested key has been generated this instant - if you don't believe me, feel free to run the program again ;)",
                exception);
        }
    }

    /// <summary>
    /// Generates a new, random key which may be used to initialize this <see cref="FixedRijndaelEncryptionKeyProvider"/>
    /// </summary>
    public static string GenerateNewKey()
    {
        using var rijndael = new RijndaelManaged();

        rijndael.GenerateKey();

        return Convert.ToBase64String(rijndael.Key);
    }

    /// <summary>
    /// Returns the key the provider was constructed with.
    /// </summary>
    /// <returns>An<see cref="EncryptionKey"/> containing the key and its identifier</returns>
    public Task<EncryptionKey> GetCurrentKey() => _encryptionKey;

    /// <summary>
    /// Returns a key matching the <paramref name="identifier"/> if found.
    /// </summary>
    /// <param name="identifier">Identifier describing which key caller is asking for.</param>
    /// <returns>An<see cref="EncryptionKey"/> containing the key and its identifier</returns>
    public Task<EncryptionKey> GetSpecificKey(string identifier) => _encryptionKey;
}