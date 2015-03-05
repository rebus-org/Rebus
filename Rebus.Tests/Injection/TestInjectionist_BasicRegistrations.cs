using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Injection;

namespace Rebus.Tests.Injection
{
    [TestFixture]
    public class TestInjectionist_BasicRegistrations : FixtureBase
    {
        Injectionist _injectionist;

        protected override void SetUp()
        {
            _injectionist = new Injectionist();
        }

        [Test]
        public void InstancesAreCachedWithinResolutionContext()
        {
            _injectionist.Register(c => new ClassWithDependencies(c.Get<Dependency>(), c.Get<Dependency>()));
            _injectionist.Register(c => new Dependency());

            var classWithDependencies = _injectionist.Get<ClassWithDependencies>();

            Assert.That(classWithDependencies.Dependency1.Id, Is.EqualTo(classWithDependencies.Dependency2.Id));
        }

        class ClassWithDependencies
        {
            readonly Dependency _dependency1;
            readonly Dependency _dependency2;

            public ClassWithDependencies(Dependency dependency1, Dependency dependency2)
            {
                _dependency1 = dependency1;
                _dependency2 = dependency2;
            }

            public Dependency Dependency1
            {
                get { return _dependency1; }
            }

            public Dependency Dependency2
            {
                get { return _dependency2; }
            }
        }

        class Dependency
        {
            static int _counter;
            public readonly int Id = Interlocked.Increment(ref _counter);
        }

        [Test]
        public void CannotRegisterTwoPrimaryImplementations()
        {
            _injectionist.Register(c => DateTime.Now);

            var ex = Assert.Throws<InvalidOperationException>(() => _injectionist.Register(c => DateTime.Now));

            Console.WriteLine("Got expected exception: {0}", ex);
        }

        [Test]
        public void CanDetermineWhetherServiceIsRegistered_Empty()
        {
            _injectionist.Register(context => DateTime.Now);

            Assert.That(_injectionist.Has<string>(), Is.False);
            Assert.That(_injectionist.Has<DateTime>(), Is.True);
        }

        [Test]
        public void ThrowsWhenResolvingSomethingThatHasNotBeenRegistered()
        {
            Assert.Throws<ResolutionException>(() => _injectionist.Get<string>());
        }

        [Test]
        public void CanResolveSimpleType()
        {
            _injectionist.Register(c => "hej");

            var instance = _injectionist.Get<string>();

            Assert.That(instance, Is.EqualTo("hej"));
        }

        [Test]
        public void CanResolveWithRealFactoryMethod()
        {
            _injectionist.Register(c => new ComplexType());

            var instance = _injectionist.Get<ComplexType>();

            Assert.That(instance, Is.TypeOf<ComplexType>());
        }

        [Test]
        public void InvokesFactoryMethodForEveryCall()
        {
            _injectionist.Register(c => new ComplexType());

            var firstInstance = _injectionist.Get<ComplexType>();
            var secondInstance = _injectionist.Get<ComplexType>();

            Assert.That(firstInstance, Is.Not.SameAs(secondInstance));
        }

        class ComplexType { }
    }
}