using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RabbitMQ.Client;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Extensions;
using Rebus.Logging;
using Rebus.Messages;
using Rebus.Serialization;
using Rebus.Tests;
using Rebus.Tests.Extensions;

#pragma warning disable 1998

namespace Rebus.RabbitMq.Tests
{
    [TestFixture]
    public class ReceiveNonRebusMessageWithRabbitMq : FixtureBase
    {
        const string ConnectionString = RabbitMqTransportFactory.ConnectionString;
        readonly string _inputQueueName = TestConfig.QueueName("custom-msg");
        BuiltinHandlerActivator _activator;

        protected override void SetUp()
        {
            RabbitMqTransportFactory.DeleteQueue(_inputQueueName);

            _activator = new BuiltinHandlerActivator();

            Using(_activator);

            Configure.With(_activator)
                .Logging(l => l.Console(LogLevel.Warn))
                .Transport(t => t.UseRabbitMq(ConnectionString, _inputQueueName))
                .Serialization(s => s.Decorate(c => new Utf8Fallback(c.Get<ISerializer>())))
                .Start();
        }

        [Test]
        public void CanReceiveNonRebusMessage()
        {
            var receivedCustomStringMessage = new ManualResetEvent(false);

            _activator.Handle<string>(async str =>
            {
                if (str != "hej med dig min ven")
                {
                    throw new ApplicationException($"Unexpected message: {str}");
                }
                receivedCustomStringMessage.Set();
            });

            using (var connection = new ConnectionFactory { Uri = ConnectionString }.CreateConnection())
            {
                using (var model = connection.CreateModel())
                {
                    var headers = model.CreateBasicProperties();
                    var body = Encoding.UTF8.GetBytes("hej med dig min ven");
                    model.BasicPublish("RebusDirect", _inputQueueName, headers, body);
                }
            }

            receivedCustomStringMessage.WaitOrDie(TimeSpan.FromSeconds(3));
        }

        class Utf8Fallback : ISerializer
        {
            readonly ISerializer _innerSerializer;

            public Utf8Fallback(ISerializer innerSerializer)
            {
                _innerSerializer = innerSerializer;
            }

            public async Task<TransportMessage> Serialize(Message message)
            {
                return await _innerSerializer.Serialize(message);
            }

            public async Task<Message> Deserialize(TransportMessage transportMessage)
            {
                try
                {
                    return await _innerSerializer.Deserialize(transportMessage);
                }
                catch
                {
                    var headers = transportMessage.Headers.Clone();
                    var body = transportMessage.Body;
                    var stringBody = Encoding.UTF8.GetString(body);
                    return new Message(headers, stringBody);
                }
            }
        }
    }
}