using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
// ReSharper disable UnusedMember.Global

namespace Rebus.Encryption;

/// <summary>
/// Helps with encrypting/decrypting byte arrays, using the <see cref="RijndaelManaged"/> algorithm (which is actually AES with 256 bits key size)
/// </summary>
sealed class RijndaelEncryptor : IAsyncEncryptor
{
    readonly IEncryptionKeyProvider _keyProvider;

    /// <summary>
    /// Returns "rijndael" string
    /// </summary>
    public string ContentEncryptionValue => "rijndael";

    /// <summary>
    /// Creates the encryptor with the specified key - the key must be a valid, base64-encoded key
    /// </summary>
    public RijndaelEncryptor(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        _keyProvider = new FixedRijndaelEncryptionKeyProvider(key);
    }

    /// <summary>
    /// Creates the encryptor with an <see cref="IEncryptionKeyProvider"/> which provides current encryption key and lookup for keys based on id
    /// </summary>
    public RijndaelEncryptor(IEncryptionKeyProvider keyProvider)
    {
        _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
    }

    /// <summary>
    /// Encrypts the given array of bytes, using the configured key. Returns an <see cref="EncryptedData"/> containing the encrypted
    /// bytes and the generated salt.
    /// </summary>
    public async Task<EncryptedData> Encrypt(byte[] bytes)
    {
        var key = await _keyProvider.GetCurrentKey();
        
        using var rijndael = new RijndaelManaged();

        rijndael.GenerateIV();
        rijndael.Key = key.Key;

        using var destination = new MemoryStream();
        using var encryptor = rijndael.CreateEncryptor();
        using var cryptoStream = new CryptoStream(destination, encryptor, CryptoStreamMode.Write);

        await cryptoStream.WriteAsync(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();

        return new EncryptedData(destination.ToArray(), rijndael.IV, key.Identifier);
    }

    /// <summary>
    /// Decrypts the given <see cref="EncryptedData"/> using the configured key.
    /// </summary>
    public async Task<byte[]> Decrypt(EncryptedData encryptedData)
    {
        var key = await _keyProvider.GetSpecificKey(encryptedData.KeyId);
        var iv = encryptedData.Iv;
        var bytes = encryptedData.Bytes;

        using var rijndael = new RijndaelManaged();

        rijndael.IV = iv;
        rijndael.Key = key.Key;

        using var destination = new MemoryStream();
        using var decryptor = rijndael.CreateDecryptor();
        using var cryptoStream = new CryptoStream(destination, decryptor, CryptoStreamMode.Write);

        await cryptoStream.WriteAsync(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();

        return destination.ToArray();
    }
}