using System;
using NUnit.Framework;
using Rebus.Encryption;

namespace Rebus.Tests.Encryption
{
    [TestFixture]
    public class TestEncryptor : FixtureBase
    {
        [Test]
        public void CanSuggestNewKeyIfInitializedWithErronousKey()
        {
            var argumentException = Assert.Throws<ArgumentException>(() =>
            {
                new Encryptor("not a valid key");
            });

            Console.WriteLine(argumentException);
        }
    }
}