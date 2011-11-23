using System.Reflection;
using NUnit.Framework;
using Rebus.Configuration.Configurers;
using Rhino.Mocks;

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

        class ThisClassNameIsPrettyRecognizable : IHandleMessages<string>
        {
            public void Handle(string message)
            {

            }
        }
    }
}