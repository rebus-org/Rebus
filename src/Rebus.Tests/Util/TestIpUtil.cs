using System;
using System.Data.SqlClient;
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

        [TestCase("Server=localhost;Database=myDataBase;Trusted_Connection=True;")] //server
        [TestCase(@"Data Source=localhost;Initial Catalog=myDataBase;Integrated Security=SSPI;User ID=myDomain\myUsername;Password=myPassword;")] //data source
        public void TestIsLocalIpAddress_ConnectionString(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            IpUtil.Lookup.IsLocalIpAddress(builder).ShouldBe(true);
        }

        [TestCase("mongodb://localhost/tradingHub")] // mongo connection string
        [TestCase("http://localhost:8870/")] // normal uri
        public void TestIsLocalIpAddress_Uri(string hostname)
        {
            IpUtil.Lookup.IsLocalIpAddress(new Uri(hostname)).ShouldBe(true);
        }

        [Test]
        public void TestIsLocalIpAddress_WithLocalHostName()
        {
            var hostname = Dns.GetHostName();
            IpUtil.Lookup.IsLocalIpAddress(hostname).ShouldBe(true);
        }
    }
}