using NUnit.Framework;
using Rebus2.Injection;

namespace Tests.Injection
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