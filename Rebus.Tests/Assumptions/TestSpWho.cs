using System;
using System.Linq;
using NUnit.Framework;

namespace Rebus.Tests.Assumptions
{
    [TestFixture]
    public class TestSpWho
    {
        [Test]
        public void DropTableThatDoesNotExist()
        {
            SqlTestHelper.DropTable("bimse");
        }

        [Test]
        public void CanGetActiveConnections()
        {
            var who = SqlTestHelper.ExecSpWho();

            Console.WriteLine(string.Join(Environment.NewLine,
                who.Select(d => string.Join(", ", d.Select(kvp => string.Format("{0} = {1}", kvp.Key, kvp.Value))))));

        }
    }
}