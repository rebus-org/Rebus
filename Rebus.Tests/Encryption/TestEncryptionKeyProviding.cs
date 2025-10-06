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
        var provider = new FixedRijndaelEncryptionKeyProvider(EncryptionKey);
        var provided = await provider.GetCurrentKey();
        Assert.That(Convert.ToBase64String(provided.Key), Is.EqualTo(EncryptionKey));
        Assert.That(provided.Identifier, Is.Not.Empty);

        var specificProvided = await provider.GetSpecificKey(provided.Identifier);
        Assert.That(Convert.ToBase64String(specificProvided.Key), Is.EqualTo(EncryptionKey));
        Assert.That(specificProvided.Identifier, Is.EqualTo(provided.Identifier));
    }

    [Test]
    public void ProviderOnlyToleratesValidKey()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            // ReSharper disable once ObjectCreationAsStatement
            new FixedRijndaelEncryptionKeyProvider(EncryptionKey.Take(10).ToString());
        });

    }
    
    
    
}