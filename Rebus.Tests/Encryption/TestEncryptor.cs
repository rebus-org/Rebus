using System;
using NUnit.Framework;
using Rebus.Encryption;
using Rebus.Tests.Contracts;

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
#if NET45
                new RijndaelEncryptor("not a valid key");
#elif NETSTANDARD1_3
                new AesEncryptor("not a valid key");
#endif

            });

            Console.WriteLine(argumentException);
        }
    }
}