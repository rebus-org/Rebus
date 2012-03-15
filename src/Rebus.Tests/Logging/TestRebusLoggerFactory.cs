using System.Reflection;
using NUnit.Framework;
using Rebus.Logging;
using Shouldly;

namespace Rebus.Tests.Logging
{
    [TestFixture]
    public class TestRebusLoggerFactory
    {
        [Test]
        public void CanChangeLoggerAnytime()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(true);

            SomeClass.Log.GetType().Name.ShouldBe("ConsoleLogger");

            RebusLoggerFactory.Current = new NullLoggerFactory();

            SomeClass.Log.GetType().Name.ShouldBe("NullLogger");
        }

        class SomeClass
        {
            public static ILog Log;

            static SomeClass()
            {
                RebusLoggerFactory.Changed += f => Log = f.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            }
        }
    }
}