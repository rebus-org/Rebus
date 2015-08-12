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

        [Test, Description("Just to be sure that the right Thread.CurrentPrincipal is available when the container must resolve the handlers")]
        public void PrincipalHasBeenEstablishedAtTheTimeWhenHandlersAreResolved()
        {
            // arrange
            adapter.Register(typeof(JustAHandlerThatCanSnatchThePrincipal));

            var message = new SomeMessage();
            bus.AttachHeader(message, Headers.UserName, "joe");

            // act
            bus.SendLocal(message);
            Thread.Sleep(1.Seconds());

            // assert
            var snatchedPrincipal = JustAHandlerThatCanSnatchThePrincipal.CurrentPrincipal;
            snatchedPrincipal.ShouldNotBe(null);
            snatchedPrincipal.Identity.Name.ShouldBe("joe");
        }

        class JustAHandlerThatCanSnatchThePrincipal : IHandleMessages<SomeMessage>
        {
            public JustAHandlerThatCanSnatchThePrincipal()
            {
                CurrentPrincipal = Thread.CurrentPrincipal;
            }

            public static IPrincipal CurrentPrincipal { get; private set; }

            public void Handle(SomeMessage message)
            {
            }
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
            resetEvent.WaitOne(1000);

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