using Microsoft.Practices.Unity;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Configuration;
using Rebus.Logging;
using Rebus.Unity;
using Shouldly;

namespace Rebus.Tests.Bugs
{
    [TestFixture, Description("got something like 'cannot resolve an interface' or something, during the configuration spell, and it was related to IStartableBus somehow")]
    public class CanDoTheFullConfigurationSpellWithUnity : FixtureBase
    {
        UnityContainer unityContainer;

        protected override void DoSetUp()
        {
            RebusLoggerFactory.Current = new ConsoleLoggerFactory(false);
            unityContainer = new UnityContainer();
        }
        
        protected override void DoTearDown()
        {
            unityContainer.Dispose();
        }
        
        [Test]
        public void DoesNotThrow()
        {
            Configure.With(new UnityContainerAdapter(unityContainer))
                .MessageOwnership(d => d.Use(Mock<IDetermineMessageOwnership>()))
                .Transport(t =>
                    {
                        t.UseReceiver(Mock<IReceiveMessages>());
                        t.UseSender(Mock<ISendMessages>());
                        t.UseErrorTracker(Mock<IErrorTracker>());
                    })
                .CreateBus()
                .Start();

            var bus = unityContainer.Resolve<IBus>();

            bus.ShouldBeOfType<RebusBus>();
        }
    }
}