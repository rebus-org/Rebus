using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Encryption;

namespace Rebus.Tests.Encryption;

[TestFixture]
public class TestEncryptionKeyProviding
{
    const string EncryptionKey = "UaVcj0zCA35mgrg9/pN62Rp+r629BMi9S9v0Tz4S7EM=";

    [Test]
    public async Task ProviderCanProvide()
    {
        var provider = new DefaultRijndaelEncryptionKeyProvider(EncryptionKey);
        var provided = await provider.GetCurrentKey();
        Assert.AreEqual(Convert.ToBase64String(provided.Key), EncryptionKey);
        Assert.IsNotEmpty(provided.Identifier);

        var specificProvided = await provider.GetSpecificKey(provided.Identifier);
        Assert.AreEqual(Convert.ToBase64String(specificProvided.Key), EncryptionKey);
        Assert.AreEqual(specificProvided.Identifier,provided.Identifier);
    }

    [Test]
    public void ProviderOnlyToleratesValidKey()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            // ReSharper disable once ObjectCreationAsStatement
            new DefaultRijndaelEncryptionKeyProvider(EncryptionKey.Take(10).ToString());
        });

    }
    
    
    
}