using System;
using Rebus.Encryption;
using Rebus.Tests.Contracts;
using Xunit;

namespace Rebus.Tests.Encryption
{
    public class TestEncryptor : FixtureBase
    {
        [Fact]
        public void CanSuggestNewKeyIfInitializedWithErronousKey()
        {
            var argumentException = Assert.Throws<ArgumentException>(() =>
            {
                new AesEncryptor("not a valid key");
            });

            Console.WriteLine(argumentException);
        }
    }
}