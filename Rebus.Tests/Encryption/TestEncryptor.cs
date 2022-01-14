using System;
using NUnit.Framework;
using Rebus.Encryption;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.Encryption;

[TestFixture]
public class TestEncryptor : FixtureBase
{
    [Test]
    public void CanSuggestNewKeyIfInitializedWithErronousKey()
    {
        var argumentException = Assert.Throws<ArgumentException>(() =>
        {
            new RijndaelEncryptor("not a valid key");
        });

        Console.WriteLine(argumentException);
    }
}