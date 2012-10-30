using NUnit.Framework;
using Rebus.Configuration;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestBuiltinContainerAdapter : FixtureBase
    {
        BuiltinContainerAdapter adapter;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();
        }

        [Test]
        public void CanRegisterOrdinaryHandlers()
        {
            // arrange
            adapter.Register(typeof (OrdinaryHandler));
            adapter.Register(typeof (AnotherOrdinaryHandler));

            // act
            var instances = adapter.GetHandlerInstancesFor<string>();

            // assert
            instances.Count().ShouldBe(2);
            instances.ShouldContain(i => i is OrdinaryHandler);
            instances.ShouldContain(i => i is AnotherOrdinaryHandler);
        }

        [Test]
        public void CanRegisterSagaHandler()
        {
            // arrange
            adapter.Register(typeof(SagaHandler));

            // act
            var instances = adapter.GetHandlerInstancesFor<string>();

            // assert
            instances.Count().ShouldBe(1);
            instances.ShouldContain(i => i is SagaHandler);
        }

        class OrdinaryHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
            }
        }

        class AnotherOrdinaryHandler : IHandleMessages<string>
        {
            public void Handle(string message)
            {
            }
        }

        class SagaHandler : IAmInitiatedBy<string>
        {
            public void Handle(string message)
            {
            }
        }
    }
}