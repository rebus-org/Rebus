using System.Text;
using NUnit.Framework;
using Rebus.Compression;

namespace Rebus.Tests.Compression
{
    [TestFixture]
    public class TestZipper
    {
        const string Text = @"Hej med dig min ven, det her er en lang tekst med en del gentagelser.
Hej med dig min ven, det her er en lang tekst med en del gentagelser.
Hej med dig min ven, det her er en lang tekst med en del gentagelser.
Hej med dig min ven, det her er en lang tekst med en del gentagelser.";

        [Test]
        public void CanRoundtripSomeBytes()
        {
            var zipper = new Zipper();

            var uncompressedBytes = Encoding.UTF8.GetBytes(Text);
            var compressedBytes = zipper.Zip(uncompressedBytes);

            Assert.That(Encoding.UTF8.GetString(zipper.Unzip(compressedBytes)), Is.EqualTo(Text));
            Assert.That(compressedBytes.Length, Is.LessThan(uncompressedBytes.Length));
        }
    }
}