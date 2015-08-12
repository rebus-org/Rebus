using NUnit.Framework;
using Rebus.Injection;

namespace Rebus.Tests.Injection
{
    [TestFixture]
    public class TestInjectionist_Decorators : FixtureBase
    {
        Injectionist _injectionist;

        protected override void SetUp()
        {
            _injectionist = new Injectionist();
        }

        [Test]
        public void CanDecorateSoSoDeep()
        {
            _injectionist.Register<ISomething>(c => new ActualSomething());
            _injectionist.Decorate<ISomething>(c => new Decorator(c.Get<ISomething>(), "4"));

            var instance = _injectionist.Get<ISomething>();

            Assert.That(instance, Is.TypeOf<Decorator>());
            Assert.That(((Decorator)instance).InnerSomething, Is.TypeOf<ActualSomething>());
        }

        [Test]
        public void CanDecorateSoSoDeepAlsoWhenRegistrationsAreMadeInOppositeOrder()
        {
            _injectionist.Decorate<ISomething>(c => new Decorator(c.Get<ISomething>(), "4"));
            _injectionist.Register<ISomething>(c => new ActualSomething());

            var instance = _injectionist.Get<ISomething>();

            Assert.That(instance, Is.TypeOf<Decorator>());
            Assert.That(((Decorator)instance).InnerSomething, Is.TypeOf<ActualSomething>());
        }

        interface ISomething { }

        class Decorator : ISomething
        {
            readonly ISomething _innerSomething;
            readonly string _id;

            public Decorator(ISomething innerSomething, string id)
            {
                _innerSomething = innerSomething;
                _id = id;
            }

            public string Id
            {
                get { return _id; }
            }

            public ISomething InnerSomething
            {
                get { return _innerSomething; }
            }
        }

        class ActualSomething : ISomething { }
    }
}