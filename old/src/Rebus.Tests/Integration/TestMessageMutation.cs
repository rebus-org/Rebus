using System;
using System.Threading;
using NUnit.Framework;
using Rebus.Configuration;
using Rebus.Transports.Msmq;
using System.Linq;
using Shouldly;

namespace Rebus.Tests.Integration
{
    [TestFixture]
    public class TestMessageMutation : FixtureBase
    {
        BuiltinContainerAdapter senderAdapter;
        BuiltinContainerAdapter receiverAdapter;
        const string SenderInputQueueName = "test.mutator.sender";
        const string ReceiverInputQueueName = "test.mutator.receiver";

        protected override void DoSetUp()
        {
            senderAdapter = new BuiltinContainerAdapter();
            Configure.With(senderAdapter)
                .Transport(t => t.UseMsmq(SenderInputQueueName, "error"))
                .Events(e =>
                    {
                        e.MessageMutators.Add(new EvilMutator("first"));
                        e.MessageMutators.Add(new EvilMutator("second"));
                        e.MessageMutators.Add(new EvilMutator("third"));
                    })
                .CreateBus().Start();

            receiverAdapter = new BuiltinContainerAdapter();
            Configure.With(receiverAdapter)
                .Transport(t => t.UseMsmq(ReceiverInputQueueName, "error"))
                .CreateBus().Start();
        }

        protected override void DoTearDown()
        {
            senderAdapter.Dispose();
            receiverAdapter.Dispose();
        }

        [Test]
        public void CanMutateMessages()
        {
            // arrange
            SomeMessageThatKeepsTrackOfMutations returnedMessage = null;
            var message = new SomeMessageThatKeepsTrackOfMutations();

            senderAdapter.Handle<SomeMessageThatKeepsTrackOfMutations>(msg => returnedMessage = msg);
            receiverAdapter.Handle<SomeMessageThatKeepsTrackOfMutations>(msg => receiverAdapter.Bus.Reply(msg));

            // act
            senderAdapter.Bus.Advanced.Routing.Send(ReceiverInputQueueName, message);

            Thread.Sleep(2.Seconds());

            // assert
            returnedMessage.ShouldNotBe(null);
            var what = string.Join(",", returnedMessage.What);
            Console.WriteLine(what);
        }

        class EvilMutator : IMutateMessages
        {
            readonly string mutatorId;

            public EvilMutator(string mutatorId)
            {
                this.mutatorId = mutatorId;
            }

            public object MutateIncoming(object message)
            {
                var mut = message as SomeMessageThatKeepsTrackOfMutations;
                if (mut == null) return message;

                return mut.MutateWith("incoming-" + mutatorId);
            }

            public object MutateOutgoing(object message)
            {
                var mut = message as SomeMessageThatKeepsTrackOfMutations;
                if (mut == null) return message;

                return mut.MutateWith("outgoing-" + mutatorId);
            }
        }

        class SomeMessageThatKeepsTrackOfMutations
        {
            public SomeMessageThatKeepsTrackOfMutations(params string[] what)
            {
                What = what;
            }

            public string[] What { get; set; }

            public SomeMessageThatKeepsTrackOfMutations MutateWith(string whatNow)
            {
                return new SomeMessageThatKeepsTrackOfMutations(What.Concat(new[] {whatNow}).ToArray());
            }
        }
    }
}