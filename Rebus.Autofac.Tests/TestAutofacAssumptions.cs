using System.Collections.Generic;
using System.Linq;
using Autofac;
using NUnit.Framework;

namespace Rebus.Autofac.Tests
{
    [TestFixture]
    public class TestAutofacAssumptions
    {
        [Test]
        public void CanResolveLotsOfStuff()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<Impl11>().As(typeof (ISomething)).InstancePerDependency();
            builder.RegisterType<Impl12>().As(typeof (ISomething)).InstancePerDependency();

            var container = builder.Build();

            var somethings = container.Resolve<IEnumerable<ISomething>>().ToArray();

            Assert.That(somethings.Length, Is.EqualTo(2));
        }

        interface ISomething { }

        class Impl11 : ISomething { }
        class Impl12 : ISomething { }
    }
}