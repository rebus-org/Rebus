using System;
using System.Security.Cryptography;

namespace Rebus.Transports.Encrypted
{
    /// <summary>
    /// Encryption helper that encapsulated the Rijndael-specified stuff. Basically just
    /// gives <see cref="RijndaelManaged"/> a decent API.
    /// </summary>
    public class RijndaelHelper
    {
        readonly byte[] key;

        public RijndaelHelper(string key)
        {
            this.key = Convert.FromBase64String(key);
        }

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