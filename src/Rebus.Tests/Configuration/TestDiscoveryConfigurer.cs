using System;
using System.Reflection;
using NUnit.Framework;
using Rebus.Configuration.Configurers;
using Rhino.Mocks;
using Shouldly;

namespace Rebus.Tests.Configuration
{
    [TestFixture]
    public class TestDiscoveryConfigurer : FixtureBase
    {
        IContainerAdapter containerAdapter;
        DiscoveryConfigurer configurer;

        protected override void DoSetUp()
        {
            containerAdapter = Mock<IContainerAdapter>();
            configurer = new DiscoveryConfigurer(containerAdapter);
        }

        [Test]
        public void CanDiscoverHandlers()
        {
            // arrange

            // act
            configurer.Handlers.LoadFrom(Assembly.GetExecutingAssembly());

            // assert
            containerAdapter.AssertWasCalled(c => c.Register(typeof (ThisClassNameIsPrettyRecognizable),
                                                             Lifestyle.Instance,
                                                             typeof (IHandleMessages<string>)));
        }

        [Test]
        public void CanFilterHandlers()
        {
            // arrange
            var wasCalled = false;

            // act
            configurer.Handlers.LoadFrom(t =>
                                             {
                                                 if (t == typeof (ThisClassNameIsPrettyRecognizable))
                                                 {
                                                     wasCalled = true;
                                                     return false;
                                                 }
                                                 return true;
                                             }, Assembly.GetExecutingAssembly());

            // assert
            containerAdapter
                .AssertWasNotCalled(c => c.Register(Arg<Type>.Is.Equal(typeof (ThisClassNameIsPrettyRecognizable)),
                                                    Arg<Lifestyle>.Is.Anything,
                                                    Arg<Type[]>.Is.Anything));

            wasCalled.ShouldBe(true);
        }

        class ThisClassNameIsPrettyRecognizable : IHandleMessages<string>
        {
            public void Handle(string message)
            {

            }
        }
    }
}