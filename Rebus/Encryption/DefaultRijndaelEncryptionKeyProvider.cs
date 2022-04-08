using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Rebus.Encryption;

/// <summary>
/// Helps with providing encryption keys for encrypting and decrypting messages.
/// </summary>
public class DefaultRijndaelEncryptionKeyProvider : IEncryptionKeyProvider
{
    private const string KeyIdentifier = "default";
    private readonly EncryptionKey _encryptionKey;

    /// <summary>
    /// Creates the keyprovider with the specified key - the key must be a valid, base64-encoded key.
    /// </summary>
    /// <param name="encryptionKey"></param>
    public DefaultRijndaelEncryptionKeyProvider(string encryptionKey)
    {
        try
        {
            _encryptionKey = new EncryptionKey(Convert.FromBase64String(encryptionKey), KeyIdentifier);

            using var rijndael = new RijndaelManaged();
            rijndael.Key = _encryptionKey.Key;
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

    static string GenerateNewKey()
    {
        using var rijndael = new RijndaelManaged();

        rijndael.GenerateKey();

        return Convert.ToBase64String(rijndael.Key);
    }

    /// <summary>
    /// Returns the key the provider was constructed with.
    /// </summary>
    /// <returns>An<see cref="EncryptionKey"/> containing the key and its identifier</returns>
    public Task<EncryptionKey> GetCurrentKey()
    {
        return Task.FromResult(_encryptionKey);
    }

    /// <summary>
    /// Returns a key matching the <see cref="identifier"/> if found.
    /// </summary>
    /// <param name="identifier">Identifier describing which key caller is asking for.</param>
    /// <returns>An<see cref="EncryptionKey"/> containing the key and its identifier</returns>
    /// <exception cref="ArgumentException">Throws if the provider has no key matching the specified <see cref="identifier"/></exception>.
    public Task<EncryptionKey> GetSpecificKey(string identifier)
    {
        if (identifier != _encryptionKey.Identifier)
            throw new ArgumentException(
                $"The {nameof(DefaultRijndaelEncryptionKeyProvider)} only provides a single key with identifier {KeyIdentifier}");
        return Task.FromResult(_encryptionKey);
    }
}