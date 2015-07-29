using System;
using System.Security.Cryptography;

namespace Rebus.Transports.Encrypted
{
    /// <summary>
    /// Encryption helper that encapsulated the Rijndael-specified stuff. Basically just
    /// gives <see cref="RijndaelManaged"/> a decent API.
    /// </summary>
    class RijndaelHelper
    {
        readonly byte[] key;

        /// <summary>
        /// Constructs the encryption helper, storing the specified key to be used when encrypting/decrypting
        /// </summary>
        public RijndaelHelper(string key)
        {
            this.key = Convert.FromBase64String(key);
        }

        /// <summary>
        /// Encrypts the specified buffer using the stored key and the specified salt and returns an encrypted buffer
        /// </summary>
        public byte[] Encrypt(byte[] bytes, string initializationVector)
        {
            var rijndael = new RijndaelManaged
                {
                    Key = key,
                    IV = Convert.FromBase64String(initializationVector),
                };

            var encryptor = rijndael.CreateEncryptor();

            return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Decrypts the specified buffer using the stored key and the specified salt and returns an unencrypted buffer
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="initializationVector"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] bytes, string initializationVector)
        {
            var rijndael = new RijndaelManaged
            {
                Key = key,
                IV = Convert.FromBase64String(initializationVector),
            };

            var decryptor = rijndael.CreateDecryptor();

            return decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Generates a new key
        /// </summary>
        public static string GenerateNewKey()
        {
            var rijndael = new RijndaelManaged
                {
                    Mode = CipherMode.CBC,
                    KeySize = 256,
                };

            rijndael.GenerateKey();

            return Convert.ToBase64String(rijndael.Key);
        }

        /// <summary>
        /// Generates a new salt
        /// </summary>
        public string GenerateNewIv()
        {
            var rijndael = new RijndaelManaged
                {
                    Key = key
                };

            rijndael.GenerateIV();

            return Convert.ToBase64String(rijndael.IV);
        }
    }
}