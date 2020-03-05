using System;
using NUnit.Framework;
using Rebus.Serialization.Custom;
using Rebus.Tests.Contracts;

namespace Rebus.Tests.Serialization
{
    [TestFixture]
    public class TestCustomTypeNameConvention : FixtureBase
    {
        [Test]
        public void CheckPrettyErrors()
        {
            var convention = new CustomTypeNameConventionBuilder().GetConvention();
            
            Console.WriteLine(
                Assert.Throws<ArgumentException>(() => convention.GetTypeName(typeof(string)))
            );

            Console.WriteLine(
                Assert.Throws<ArgumentException>(() => convention.GetType("string"))
            );
        }
    }
}