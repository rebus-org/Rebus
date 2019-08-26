using System;
using NUnit.Framework;
using Rebus.Injection;

namespace Rebus.Tests.Injection
{
    [TestFixture]
    public class TestInjectionist
    {
        [Test]
        public void ThrowsNiceExceptionWhenRequestingNonExistentService()
        {
            var injectionist = new Injectionist();

            var exception = Assert.Throws<ResolutionException>(() => injectionist.Get<string>());

            Console.WriteLine(exception);

            Assert.That(exception.ToString(), Contains.Substring("System.String"));
        }

        [Test]
        public void CanGetDependencyInjected()
        {
            var injectionist = new Injectionist();

            injectionist.Register(c => "hej " + c.Get<int>());
            injectionist.Register(c => 23);

            var resolutionResult = injectionist.Get<string>();

            Assert.That(resolutionResult.Instance, Is.EqualTo("hej 23"));
        }

        [Test]
        public void CanDelegateResolutionRequestToParentContainer()
        {
            var parent = new Injectionist();
            parent.Register(c => 23);

            var child = new Injectionist(parent);
            child.Register(c => "hej " + c.Get<int>());
            
            var resolutionResult = child.Get<string>();

            Assert.That(resolutionResult.Instance, Is.EqualTo("hej 23"));
        }
    }
}