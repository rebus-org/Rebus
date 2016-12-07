using System;
using System.IO;
using System.Security.Cryptography;

namespace Rebus.Encryption
{
    /// <summary>
    /// Helps with encrypting/decripting byte arrays, using the <see cref="RijndaelManaged"/> algorithm
    /// </summary>
    class AesEncryptor : IEncryptor
    {
        readonly byte[] _key;

        /// <summary>
        /// Returns "rijndael" string
        /// </summary>
        public string ContentEncryptionValue => "rijndael";

        /// <summary>
        /// Creates the encrptor with the specified key - the key must be a valid, base64-encoded key
        /// </summary>
        /// <param name="key"></param>
        public AesEncryptor(string key)
        {
            _key = Convert.FromBase64String(key);

            try
            {
                using (var rijndael = Aes.Create())
                {
                    rijndael.Key = _key;
                }
            }
            catch (Exception exception)
            {
                throw new ArgumentException(
                    $@"Could not initialize the encryption algorithm with the specified key (not shown here for security reasons) - if you're unsure how to get a valid key, here's a newly generated key that you can use:

    {GenerateNewKey()}

I promise that the suggested key has been generated this instant - if you don't believe me, feel free to run the program again ;)", exception);
            }
        }

        static string GenerateNewKey()
        {
            using (var rijndael = Aes.Create())
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
            using (var rijndael = Aes.Create())
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

            using (var rijndael = Aes.Create())
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
}