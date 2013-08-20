using System.Security.Principal;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Shared;
using Rebus.Transports.Msmq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestUserContext : FixtureBase
    {
        const string InputQueueName = "test.currentprincipal.input";
        BuiltinContainerAdapter adapter;
        IBus bus;

        protected override void DoSetUp()
        {
            adapter = new BuiltinContainerAdapter();

            MsmqUtil.PurgeQueue(InputQueueName);

            Configure.With(adapter)
                     .Transport(t => t.UseMsmq(InputQueueName, "error"))
                     .Behavior(b => b.SetCurrentPrincipalWhenUserNameHeaderIsPresent())
                     .CreateBus()
                     .Start();

            bus = adapter.Bus;
        }

        protected override void DoTearDown()
        {
            adapter.Dispose();
        }

        [Test]
        public void EstablishesCurrentPrincipalWhenHandledMessageHasUserName()
        {
            // arrange
            IPrincipal currentPrincipal = null;
            var resetEvent = new ManualResetEvent(false);
            adapter.Handle<SomeMessage>(m =>
                {
                    // snatch it!
                    currentPrincipal = Thread.CurrentPrincipal;
                    resetEvent.Set();
                });

            var message = new SomeMessage();
            bus.AttachHeader(message, Headers.UserName, "mownz");

            // act
            bus.SendLocal(message);
            resetEvent.WaitOne();

            // assert
            currentPrincipal.ShouldNotBe(null);
            currentPrincipal.Identity.Name.ShouldBe("mownz");
        }

        [Test]
        public void HasDefaultUserContextWhenNoUserNameHeaderIsPresent()
        {
            // arrange
            IPrincipal currentPrincipal = null;
            var resetEvent = new ManualResetEvent(false);
            adapter.Handle<SomeMessage>(m =>
                {
                    // snatch it!
                    currentPrincipal = Thread.CurrentPrincipal;
                    resetEvent.Set();
                });

            var message = new SomeMessage();

            // act
            bus.SendLocal(message);
            resetEvent.WaitOne();

            // assert
            currentPrincipal.ShouldNotBe(null);
            currentPrincipal.Identity.Name.ShouldBe("");
        }

        class SomeMessage { }
    }
}