using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Tests.Contracts.Transports.Factories;
using Shouldly;

namespace Rebus.Tests.Contracts.Transports
{
    [TestFixture(typeof(MsmqTransportFactory))]
    public class TestTransactionality<TFactory> : FixtureBase where TFactory : ITransportFactory, new()
    {
        static readonly Encoding Encoding = Encoding.UTF8;

        TFactory factory;
        ISendMessages sender;
        IReceiveMessages receiver;

        protected override void DoSetUp()
        {
            factory = new TFactory();

            var transports = factory.Create();
            sender = transports.Item1;
            receiver = transports.Item2;
        }

        protected override void DoTearDown()
        {
            factory.CleanUp();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CanReceiveAndDoMultipleSendsAtomically(bool commitTransactionAndExpectMessagesToBeThere)
        {
            sender.Send(receiver.InputQueueAddress, MessageWith("hello"));

            var destination1 = factory.CreateReceiver("destination1");
            var destination2 = factory.CreateReceiver("destination2");

            Thread.Sleep(300);

            // pretend that this is a message handler tx scope...
            using (var tx = new TransactionScope())
            {
                // arrange
                var receivedTransportMessage = receiver.ReceiveMessage();

                // act
                sender.Send(destination1.InputQueueAddress, receivedTransportMessage.ToForwardableMessage());
                sender.Send(destination2.InputQueueAddress, receivedTransportMessage.ToForwardableMessage());

                if (commitTransactionAndExpectMessagesToBeThere) tx.Complete();
            }

            Thread.Sleep(300);

            // assert
            var msg1 = destination1.ReceiveMessage();
            var msg2 = destination2.ReceiveMessage();

            if (commitTransactionAndExpectMessagesToBeThere)
            {
                msg1.ShouldNotBe(null);
                Encoding.GetString(msg1.Body).ShouldBe("hello");

                msg2.ShouldNotBe(null);
                Encoding.GetString(msg2.Body).ShouldBe("hello");

                receiver.ReceiveMessage().ShouldBe(null);
            }
            else
            {
                msg1.ShouldBe(null);
                msg2.ShouldBe(null);
                receiver.ReceiveMessage().ShouldNotBe(null);
            }
        }

        TransportMessageToSend MessageWith(string text)
        {
            return new TransportMessageToSend {Body = Encoding.GetBytes(text)};
        }
    }
}