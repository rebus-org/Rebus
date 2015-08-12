using NUnit.Framework;
using Rebus.Injection;

namespace Rebus.Tests.Injection
{
    [TestFixture]
    public class TestInjectionist_ServiceAndImplementation : FixtureBase
    {
        Injectionist _injectionist;

        protected override void SetUp()
        {
            _injectionist = new Injectionist();
        }

        [Test]
        public void CanResolveViaRegisteredInterface()
        {
            _injectionist.Register<ISomeDependency>(c => new SomeDependency());
            _injectionist.Register<ISomeService>(c => new SomeImplementation(c.Get<ISomeDependency>()));

            var instance = _injectionist.Get<ISomeService>();

            Assert.That(instance, Is.TypeOf<SomeImplementation>());
            Assert.That(((SomeImplementation)instance).SomeDependency, Is.TypeOf<SomeDependency>());
        }

        interface ISomeService { }

        interface ISomeDependency { }

        class SomeImplementation : ISomeService
        {
            readonly ISomeDependency _someDependency;

            public SomeImplementation(ISomeDependency someDependency)
            {
                _someDependency = someDependency;
            }

            public ISomeDependency SomeDependency
            {
                get { return _someDependency; }
            }
        }

        class SomeDependency : ISomeDependency { }
    }

}