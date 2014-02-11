using System.Net;
using NUnit.Framework;
using Shouldly;

namespace Rebus.Tests.Util
{
    [TestFixture]
    public class TestIpUtil
    {
        [TestCase("localhost", true)] // true (loopback name)
        [TestCase("127.0.0.1", true)] // true (loopback IP)
        [TestCase("NonExistingName", false)] // false (non existing computer name)
        [TestCase("99.0.0.1", false)]
        public void TestIsLocalIpAddress(string hostname, bool isLocal)
        {
            IpUtil.Lookup.IsLocalIpAddress(hostname).ShouldBe(isLocal);
        }

        [Test]
        public void TestIsLocalIpAddress_WithLocalHostName()
        {
            var hostname = Dns.GetHostName();
            IpUtil.Lookup.IsLocalIpAddress(hostname).ShouldBe(true);
        }
    }
}