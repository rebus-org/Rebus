using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Messages;
using Rebus.Transport;

namespace Rebus.Tests.Contracts.Transports
{
    public class BasicSendReceive<TTransportFactory> : FixtureBase where TTransportFactory : ITransportFactory, new()
    {
        readonly Encoding _defaultEncoding = Encoding.UTF8;

        TTransportFactory _factory;

        protected override void SetUp()
        {
            _factory = new TTransportFactory();
        }

        protected override void TearDown()
        {
            _factory.CleanUp();
        }

        [Test]
        public async Task EmptyQueueReturnsNull()
        {
            var emptyQueue = _factory.Create("empty");

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await emptyQueue.Receive(context);

                Assert.That(transportMessage, Is.Null);

                context.Complete();
            }
        }

        [Test]
        public async Task CanSendAndReceive()
        {
            var input1 = _factory.Create("input1");
            var input2 = _factory.Create("input2");

            using (var context = new DefaultTransactionContext())
            {
                await input1.Send("input2", MessageWith("hej"), context);
                
                context.Complete();
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await input2.Receive(context);
                var stringBody = GetStringBody(transportMessage);

                Assert.That(stringBody, Is.EqualTo("hej"));

                context.Complete();
            }
        }

        [Test]
        public async Task MessageIsNotSentWhenTransactionIsNotCompleted()
        {
            var input1 = _factory.Create("input1");
            var input2 = _factory.Create("input2");

            using (var context = new DefaultTransactionContext())
            {
                await input1.Send("input2", MessageWith("hej"), context);

                //< we don't complete the transaction   //context.Complete(); 
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await input2.Receive(context);

                Assert.That(transportMessage, Is.Null);

                context.Complete();
            }
        }

        [Test]
        public async Task MessageIsReturnedToQueueWhenReceivingTransactionIsNotCommitted()
        {
            var input1 = _factory.Create("input1");
            var input2 = _factory.Create("input2");

            using (var context = new DefaultTransactionContext())
            {
                await input1.Send("input2", MessageWith("hej"), context);

                context.Complete(); 
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await input2.Receive(context);
                var stringBody = GetStringBody(transportMessage);

                Assert.That(stringBody, Is.EqualTo("hej"));

                // we don't complete the transaction //    context.Complete();
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await input2.Receive(context);
                var stringBody = GetStringBody(transportMessage);

                Assert.That(stringBody, Is.EqualTo("hej"));

                context.Complete();
            }

            using (var context = new DefaultTransactionContext())
            {
                var transportMessage = await input2.Receive(context);

                Assert.That(transportMessage, Is.Null);

                context.Complete();
            }
        }

        string GetStringBody(TransportMessage transportMessage)
        {
            using (var reader = new StreamReader(transportMessage.Body, _defaultEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        TransportMessage MessageWith(string stringBody)
        {
            return new TransportMessage(new Dictionary<string, string>(), new MemoryStream(_defaultEncoding.GetBytes(stringBody)));
        }
    }
}