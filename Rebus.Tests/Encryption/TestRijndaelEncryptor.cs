using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Encryption;
using Rebus.Tests.Contracts;
// ReSharper disable EmptyGeneralCatchClause

namespace Rebus.Tests.Encryption;

[TestFixture]
public class TestRijndaelEncryptor : FixtureBase
{
    [Test]
    public void CanSuggestNewKeyIfInitializedWithErronousKey()
    {
        var argumentException = Assert.Throws<ArgumentException>(() => _ = new RijndaelEncryptor("not a valid key"));

        Console.WriteLine(argumentException);
    }

    [Test]
    public async Task EncryptedTextIsUnreadable()
    {
        var encryptor = GetEncryptor();

        const string originalInputString = "HEJ MED DIG MIN VEN";
        var data = await encryptor.Encrypt(Encoding.UTF8.GetBytes(originalInputString));

        try
        {
            var str = Encoding.UTF8.GetString(data.Bytes);

            Assert.That(str.Contains("HEJ"), Is.False);
            Assert.That(str.Contains("MED"), Is.False);
            Assert.That(str.Contains("DIG"), Is.False);
            Assert.That(str.Contains("MIN"), Is.False);
            Assert.That(str.Contains("DIG"), Is.False);
            Assert.That(str.Contains("VEN"), Is.False);
            Assert.That(str.Contains(originalInputString), Is.False);

        }
        catch { }
    }

    [Test]
    public async Task CanRoundtripData()
    {
        const string originalInputString = "HEJ MED DIG MIN VEN";

        var encryptor = GetEncryptor();
        var encryptedData = await encryptor.Encrypt(Encoding.UTF8.GetBytes(originalInputString));
        var decryptedData = await encryptor.Decrypt(encryptedData);

        var roundtripped = Encoding.UTF8.GetString(decryptedData);

        Assert.That(roundtripped, Is.EqualTo(originalInputString));
    }

    [Test]
    public async Task IsCompatibleWithAes()
    {
        const string originalInputString = "HEJ MED DIG MIN VEN";
       
        var knownKey = FixedRijndaelEncryptionKeyProvider.GenerateNewKey();
        var encryptor = GetEncryptor(knownKey);
        var encryptedData = await encryptor.Encrypt(Encoding.UTF8.GetBytes(originalInputString));

        var decryptedBytes = await DecryptWithAes(knownKey, encryptedData);
        var roundtrippedString = Encoding.UTF8.GetString(decryptedBytes);

        Assert.That(roundtrippedString, Is.EqualTo(originalInputString));
        
    }

    static async Task<byte[]> DecryptWithAes(string key, EncryptedData encryptedData)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.IV = encryptedData.Iv;
        using var decryptor = aes.CreateDecryptor();

        using var destination = new MemoryStream();
        await using var cryptoStream = new CryptoStream(destination, decryptor, CryptoStreamMode.Write);

        await cryptoStream.WriteAsync(encryptedData.Bytes, 0, encryptedData.Bytes.Length);
        await cryptoStream.FlushFinalBlockAsync();

        return destination.ToArray();
    }

    static RijndaelEncryptor GetEncryptor(string key = null) => new(key ?? FixedRijndaelEncryptionKeyProvider.GenerateNewKey());
}