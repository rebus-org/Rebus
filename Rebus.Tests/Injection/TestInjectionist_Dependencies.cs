using NUnit.Framework;
using Rebus.Injection;

namespace Rebus.Tests.Injection
{
    [TestFixture]
    public class TestInjectionist_Dependencies : FixtureBase
    {
        Injectionist _injectionist;

        protected override void SetUp()
        {
            _injectionist = new Injectionist();
        }

        [Test]
        public void CanResolveInstanceWithDependency()
        {
            _injectionist.Register(c => new SomeDependency());
            _injectionist.Register(c => new SomeClass(c.Get<SomeDependency>()));

            var instance = _injectionist.Get<SomeClass>();

            Assert.That(instance, Is.TypeOf<SomeClass>());
            Assert.That(instance.Dependency, Is.TypeOf<SomeDependency>());
        }

        class SomeClass
        {
            readonly SomeDependency _dependency;

            public SomeClass(SomeDependency dependency)
            {
                _dependency = dependency;
            }

            public SomeDependency Dependency
            {
                get { return _dependency; }
            }
        }

        class SomeDependency
        {
            
        }

        [Test]
        public void WorksWithMultipleDependencies()
        {
            _injectionist.Register(c => new SomeClassWithMultipleDependencies(c.Get<SomeClass>(), c.Get<OtherDependency>()));
            _injectionist.Register(c => new SomeDependency());
            _injectionist.Register(c => new OtherDependency());
            _injectionist.Register(c => new SomeClass(c.Get<SomeDependency>()));

            var instance = _injectionist.Get<SomeClassWithMultipleDependencies>();

            Assert.That(instance, Is.TypeOf<SomeClassWithMultipleDependencies>());
            Assert.That(instance.FirstDependency, Is.TypeOf<SomeClass>());
            Assert.That(instance.FirstDependency.Dependency, Is.TypeOf<SomeDependency>());
            Assert.That(instance.OtherDependency, Is.TypeOf<OtherDependency>());
        }

        class SomeClassWithMultipleDependencies
        {
            readonly SomeClass _firstDependency;
            readonly OtherDependency _otherDependency;

            public SomeClassWithMultipleDependencies(SomeClass firstDependency, OtherDependency otherDependency)
            {
                _firstDependency = firstDependency;
                _otherDependency = otherDependency;
            }

            public OtherDependency OtherDependency
            {
                get { return _otherDependency; }
            }

            public SomeClass FirstDependency
            {
                get { return _firstDependency; }
            }
        }

        class OtherDependency { }
    }
}