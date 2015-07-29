using System.Text;
using NUnit.Framework;
using Rebus.Transports.Encrypted;
using Shouldly;

namespace Rebus.Tests.Transports.Encrypted
{
    [TestFixture]
    public class TestRijndaelHelper : FixtureBase
    {
        RijndaelHelper helper;

        protected override void DoSetUp()
        {
            helper = new RijndaelHelper(RijndaelHelper.GenerateNewKey());
        }

        [Test]
        public void CanEncryptAndDecryptStuff()
        {
            // arrange
            const string someText = @"this is a fairly long text that contains some stuff that's genuinely unicode, like e.g. æ, ø, and å (ÆØÅ) and this thing: ñ";

            var iv = helper.GenerateNewIv();
            var encoding = Encoding.UTF8;
            var someTextBytes = encoding.GetBytes(someText);

            // act
            var encryptedBytes = helper.Encrypt(someTextBytes, iv);
            var encryptedString = encoding.GetString(encryptedBytes);

            var decryptedBytes = helper.Decrypt(encryptedBytes, iv);
            var decryptedString = encoding.GetString(decryptedBytes);

            // assert
            encryptedString.ShouldNotBe(someText);
            decryptedString.ShouldBe(someText);
        }
    }
}