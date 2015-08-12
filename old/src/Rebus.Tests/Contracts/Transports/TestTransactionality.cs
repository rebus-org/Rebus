using System.Text;
using System.Threading;
using System.Transactions;
using NUnit.Framework;
using Rebus.Bus;
using Rebus.Tests.Contracts.Transports.Factories;

namespace Rebus.Tests.Contracts.Transports
{
    [TestFixture(typeof(AzureServiceBusMessageQueueFactory), Category = TestCategories.Azure)]
    [TestFixture(typeof(MsmqTransportFactory))]
    [TestFixture(typeof(SqlServerTransportFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(RabbitMqTransportFactory), Category = TestCategories.Rabbit)]
    [TestFixture(typeof(FileSystemTransportFactory))]
    public class TestTransactionality<TFactory> : FixtureBase where TFactory : ITransportFactory, new()
    {
        readonly Encoding encoding = Encoding.UTF8;

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

        [TestCase(true, Description = "Commits the transaction and verifies that both receivers have got a message, and also that the handled message has disappeared from the input queue")]
        [TestCase(false, Description = "Rolls back the transaction and verifies that none of the receiver have got a message, and also that the handled message has been returned to the input queue")]
        public void CanReceiveAndDoOneSingleSendAtomically(bool commitTransactionAndExpectMessagesToBeThere)
        {
            sender.Send(receiver.InputQueueAddress, MessageWith("hello"), new NoTransaction());

            var destination1 = factory.CreateReceiver("destination1");

            Thread.Sleep(300.Milliseconds());

            // pretend that this is a message handler tx scope...
            using (var tx = new TransactionScope())
            {
                var ctx = new AmbientTransactionContext();

                // arrange
                var receivedTransportMessage = receiver.ReceiveMessage(ctx);
                Assert.That(receivedTransportMessage, Is.Not.Null);

                // act
                sender.Send(destination1.InputQueueAddress, MessageWith("hello mr. 1"), ctx);

                if (commitTransactionAndExpectMessagesToBeThere)
                {
                    tx.Complete();
                }
            }

            Thread.Sleep(300.Milliseconds());

            // assert
            var msg1 = destination1.ReceiveMessage(new NoTransaction());

            if (commitTransactionAndExpectMessagesToBeThere)
            {
                Assert.That(msg1, Is.Not.Null);
                Assert.That(encoding.GetString(msg1.Body), Is.EqualTo("hello mr. 1"));

                using (new TransactionScope())
                {
                    var ctx = new AmbientTransactionContext();
                    var receivedTransportMessage = receiver.ReceiveMessage(ctx);
                    Assert.That(receivedTransportMessage, Is.Null);
                }
            }
            else
            {
                Assert.That(msg1, Is.Null);

                using (new TransactionScope())
                {
                    var ctx = new AmbientTransactionContext();
                    var receivedTransportMessage = receiver.ReceiveMessage(ctx);
                    Assert.That(receivedTransportMessage, Is.Not.Null);
                    Assert.That(encoding.GetString(receivedTransportMessage.Body), Is.EqualTo("hello"));
                }
            }
        }

        [TestCase(true, Description = "Commits the transaction and verifies that both receivers have got a message, and also that the handled message has disappeared from the input queue")]
        [TestCase(false, Description = "Rolls back the transaction and verifies that none of the receiver have got a message, and also that the handled message has been returned to the input queue")]
        public void CanReceiveAndDoMultipleSendsAtomically(bool commitTransactionAndExpectMessagesToBeThere)
        {
            var destination1 = factory.CreateReceiver("destination1");
            var destination2 = factory.CreateReceiver("destination2");

            sender.Send(receiver.InputQueueAddress, MessageWith("hello"), new NoTransaction());

            Thread.Sleep(300.Milliseconds());

            // pretend that this is a message handler tx scope...
            using (var tx = new TransactionScope())
            {
                var ctx = new AmbientTransactionContext();

                // arrange
                var receivedTransportMessage = receiver.ReceiveMessage(ctx);
                Assert.That(receivedTransportMessage, Is.Not.Null);

                // act
                sender.Send(destination1.InputQueueAddress, MessageWith("hello mr. 1"), ctx);
                sender.Send(destination2.InputQueueAddress, MessageWith("hello mr. 2"), ctx);

                if (commitTransactionAndExpectMessagesToBeThere)
                {
                    tx.Complete();
                }
            }

            Thread.Sleep(300.Milliseconds());

            // assert
            var msg1 = destination1.ReceiveMessage(new NoTransaction());
            var msg2 = destination2.ReceiveMessage(new NoTransaction());

            if (commitTransactionAndExpectMessagesToBeThere)
            {
                Assert.That(msg1, Is.Not.Null);
                Assert.That(encoding.GetString(msg1.Body), Is.EqualTo("hello mr. 1"));

                Assert.That(msg2, Is.Not.Null);
                Assert.That(encoding.GetString(msg2.Body), Is.EqualTo("hello mr. 2"));

                using (new TransactionScope())
                {
                    var ctx = new AmbientTransactionContext();
                    var receivedTransportMessage = receiver.ReceiveMessage(ctx);
                    Assert.That(receivedTransportMessage, Is.Null);
                }
            }
            else
            {
                Assert.That(msg1, Is.Null);
                Assert.That(msg2, Is.Null);

                using (new TransactionScope())
                {
                    var ctx = new AmbientTransactionContext();
                    var receivedTransportMessage = receiver.ReceiveMessage(ctx);
                    Assert.That(receivedTransportMessage, Is.Not.Null);
                    Assert.That(encoding.GetString(receivedTransportMessage.Body), Is.EqualTo("hello"));
                }
            }
        }

        TransportMessageToSend MessageWith(string text)
        {
            return new TransportMessageToSend { Body = encoding.GetBytes(text) };
        }
    }
}