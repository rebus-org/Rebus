using System;
using System.IO;
using System.Security.Cryptography;

namespace Rebus.Encryption
{
    public class Encryptor
    {
        readonly byte[] _key;

        public Encryptor(string key)
        {
            _key = Convert.FromBase64String(key);

            using (var rijndael = new RijndaelManaged())
            {
                rijndael.Key = _key;
            }
        }

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

        public byte[] Decrypt(byte[] bytes, byte[] iv)
        {
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

    public class EncryptedData
    {
        public EncryptedData(byte[] bytes, byte[] iv)
        {
            Bytes = bytes;
            Iv = iv;
        }

        public byte[] Bytes { get; private set; }
        public byte[] Iv { get; private set; }
    }
}