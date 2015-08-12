using System;
using System.IO;
using System.Security.Cryptography;

namespace Rebus.Encryption
{
    /// <summary>
    /// Helps with encrypting/decripting byte arrays, using the <see cref="RijndaelManaged"/> algorithm
    /// </summary>
    public class Encryptor
    {
        readonly byte[] _key;

        /// <summary>
        /// Creates the encrptor with the specified key - the key must be a valid, base64-encoded key
        /// </summary>
        /// <param name="key"></param>
        public Encryptor(string key)
        {
            _key = Convert.FromBase64String(key);

            try
            {
                using (var rijndael = new RijndaelManaged())
                {
                    rijndael.Key = _key;
                }
            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format(@"Could not initialize the encryption algorithm with the specified key (not shown here for security reasons) - if you're unsure how to get a valid key, here's a newly generated key that you can use:

    {0}

I promise that the suggested key has been generated this instant - if you don't believe me, feel free to run the program again ;)",
                    GenerateNewKey()), exception);
            }
        }

        string GenerateNewKey()
        {
            using (var rijndael = new RijndaelManaged())
            {
                rijndael.GenerateKey();
                
                return Convert.ToBase64String(rijndael.Key);
            }
        }

        /// <summary>
        /// Encrypts the given array of bytes, using the configured key. Returns an <see cref="EncryptedData"/> containing the encrypted
        /// bytes and the generated salt.
        /// </summary>
        public EncryptedData Encrypt(byte[] bytes)
        {
            using (var rijndael = new RijndaelManaged())
            {
                rijndael.GenerateIV();
                rijndael.Key = _key;

                using (var encryptor = rijndael.CreateEncryptor())
                using (var destination = new MemoryStream())
                using (var cryptoStream = new CryptoStream(destination, encryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                    cryptoStream.FlushFinalBlock();

                    return new EncryptedData(destination.ToArray(), rijndael.IV);
                }
            }
        }

        /// <summary>
        /// Decrypts the given <see cref="EncryptedData"/> using the configured key.
        /// </summary>
        public byte[] Decrypt(EncryptedData encryptedData)
        {
            var iv = encryptedData.Iv;
            var bytes = encryptedData.Bytes;

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.IV = iv;
                rijndael.Key = _key;

                using (var decryptor = rijndael.CreateDecryptor())
                using (var destination = new MemoryStream())
                using(var cryptoStream = new CryptoStream(destination, decryptor, CryptoStreamMode.Write))
                {
                    cryptoStream.Write(bytes, 0, bytes.Length);
                    cryptoStream.FlushFinalBlock();

                    return destination.ToArray();
                }
            }
        }
    }

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
            if (bytes == null) throw new ArgumentNullException("bytes");
            if (iv == null) throw new ArgumentNullException("iv");
            Bytes = bytes;
            Iv = iv;
        }

        /// <summary>
        /// Gets the raw data from this encrypted data instance
        /// </summary>
        public byte[] Bytes { get; private set; }
        
        /// <summary>
        /// Gets the salt (a.k.a. "Initialization Vector"/"IV") from this encrypted data instance
        /// </summary>
        public byte[] Iv { get; private set; }
    }
}