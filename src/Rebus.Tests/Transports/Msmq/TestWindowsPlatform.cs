using NUnit.Framework;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Transports.Msmq
{
    [TestFixture]
    public class TestWindowsPlatform : FixtureBase
    {
        WindowsPlatform ftw;

        protected override void DoSetUp()
        {
            ftw = new WindowsPlatform();
        }

        [Test]
        public void CanLookupAdministratorAccountName()
        {
            // arrange

            // act
            var name = ftw.GetAdministratorAccountName();

            // assert
            name.ShouldBe("Administrators");
        }
    }
}