using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Rebus.Encryption;

/// <summary>
/// Helps with providing encryption keys for encrypting and decrypting messages.
/// </summary>
public class FixedAesEncryptionKeyProvider : IEncryptionKeyProvider
{
    const string KeyIdentifier = "default";
    readonly Task<EncryptionKey> _encryptionKey;

    /// <summary>
    /// Creates the keyprovider with the specified key - the key must be a valid, base64-encoded key.
    /// </summary>
    public FixedAesEncryptionKeyProvider(string encryptionKey)
    {
        try
        {
            var key = new EncryptionKey(Convert.FromBase64String(encryptionKey), KeyIdentifier);

            // verify the key - will throw if it's not a valid size/padded correctly
            using var aes = Aes.Create();
            aes.Key = key.Key;

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
    public static string GenerateNewKey(int keySize = 256)
    {
        using var aes = Aes.Create();
        try
        {
            aes.KeySize = keySize;
            aes.GenerateKey();

            return Convert.ToBase64String(aes.Key);
        }
        catch (CryptographicException exception)
        {
            throw new CryptographicException(
                $"Could not generate key with size {keySize} bits - valid sizes are: {string.Join(", ", aes.LegalKeySizes.Select(s => new { s.MinSize, s.MaxSize, s.SkipSize }))}", exception);
        }
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