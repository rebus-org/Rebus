using System.Messaging;
using System.Text;
using System.Threading;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Contracts;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Transport.Msmq
{
    [TestFixture]
    [Description("Verify sane behavior when a mangled (possibly non-Rebus) message is received")]
    public class TestMsmqMangledMessage : FixtureBase
    {
        readonly string _inputQueueName = TestConfig.GetName("mangled-message");

        protected override void SetUp()
        {
            MsmqUtil.Delete(_inputQueueName);

            Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseMsmq(_inputQueueName))
                .Start();
        }

        [Test]
        public void MangledMessageIsNotReceived()
        {
            using (var messageQueue = new MessageQueue(MsmqUtil.GetPath(_inputQueueName)))
            {
                var transaction = new MessageQueueTransaction();
                transaction.Begin();
                messageQueue.Send(new Message
                {
                    Extension = Encoding.UTF32.GetBytes("this is definitely not valid UTF8-encoded JSON")
                }, transaction);
                transaction.Commit();
            }

            Thread.Sleep(5000);

            CleanUpDisposables();

            using (var messageQueue = new MessageQueue(MsmqUtil.GetPath(_inputQueueName)))
            {
                messageQueue.MessageReadPropertyFilter = new MessagePropertyFilter
                {
                    Extension = true
                };

                var transaction = new MessageQueueTransaction();
                transaction.Begin();

                var message = messageQueue.Receive(transaction);

                Assert.That(message, Is.Not.Null);
                Assert.That(Encoding.UTF32.GetString(message.Extension), Is.EqualTo("this is definitely not valid UTF8-encoded JSON"));

                transaction.Commit();
            }
        }
    }
}