using NUnit.Framework;
using Rebus.Testing;
using Shouldly;

namespace Rebus.Tests.Testing
{
    [TestFixture]
    public class TestFakeBus : FixtureBase
    {
        FakeBus fakeBus;

        protected override void DoSetUp()
        {
            fakeBus = new FakeBus();
        }

        [Test]
        public void CanAttachHeader()
        {
            // arrange
            var foo = new { What = "whatever" };
            
            // act
            fakeBus.AttachHeader(foo, "custom-header", "ftlolz!!1");

            // assert
            fakeBus.GetAttachedHeaders(foo).ShouldContainKeyAndValue("custom-header", "ftlolz!!1");
        }
    }
}