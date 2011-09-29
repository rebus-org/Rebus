using System;
using System.ComponentModel;
using System.Threading;
using NUnit.Framework;
using Rebus.Messages;
using Rhino.Mocks;

namespace Rebus.Tests.Unit
{
    [TestFixture]
    public class TestRebusBus : FixtureBase
    {
        RebusBus bus;
        IReceiveMessages receiveMessages;
        IHandlerFactory handlerFactory;

        protected override void DoSetUp()
        {
            receiveMessages = Mock<IReceiveMessages>();
            handlerFactory = Mock<IHandlerFactory>();
            bus = new RebusBus(handlerFactory,
                               Mock<ISendMessages>(),
                               receiveMessages,
                               Mock<IStoreSubscriptions>());
        }

        [Test]
        public void CanDoPolymorphicMessageDispatch()
        {
            receiveMessages.Stub(r => r.ReceiveMessage())
                .Return(new TransportMessage
                            {
                                Messages = new object[]
                                               {
                                                   new PolymorphicMessage()
                                               }
                            });

            var manualResetEvent = new ManualResetEvent(false);

            var handler = new SomeHandler(manualResetEvent);
            
            handlerFactory.Stub(f => f.GetHandlerInstancesFor<IFirstInterface>())
                .Return(new[] {(IHandleMessages<IFirstInterface>) handler});
            
            handlerFactory.Stub(f => f.GetHandlerInstancesFor<ISecondInterface>())
                .Return(new[] {(IHandleMessages<ISecondInterface>) handler});
            
            handlerFactory.Stub(f => f.GetHandlerInstancesFor<PolymorphicMessage>())
                .Return(new IHandleMessages<PolymorphicMessage>[0]);

            bus.Start();

            if (!manualResetEvent.WaitOne(TimeSpan.FromSeconds(1)))
            {
                Assert.Fail("Did not receive messages withing timeout");
            }

            Assert.That(handler.FirstMessageHandled, Is.True);
            Assert.That(handler.SecondMessageHandled, Is.True);
        }

        class SomeHandler : IHandleMessages<IFirstInterface>, IHandleMessages<ISecondInterface>
        {
            readonly ManualResetEvent manualResetEvent;

            public SomeHandler(ManualResetEvent manualResetEvent)
            {
                this.manualResetEvent = manualResetEvent;
            }

            public bool FirstMessageHandled { get; set; }
            public bool SecondMessageHandled { get; set; }

            public void Handle(IFirstInterface message)
            {
                FirstMessageHandled = true;
                PossiblyRaiseEvent();
            }

            public void Handle(ISecondInterface message)
            {
                SecondMessageHandled = true;
                PossiblyRaiseEvent();
            }

            void PossiblyRaiseEvent()
            {
                if (FirstMessageHandled && SecondMessageHandled)
                {
                    manualResetEvent.Set();
                }
            }
        }

        interface IFirstInterface {}
        interface ISecondInterface {}
        class PolymorphicMessage : IFirstInterface, ISecondInterface{}
    }
}