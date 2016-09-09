using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Routing.TypeBased;
using Rebus.Tests.Contracts;
using Rebus.Tests.Contracts.Extensions;

#pragma warning disable 1998

namespace Rebus.Msmq.Tests.Integration
{
    [TestFixture]
    public class TestOneWayClient : FixtureBase
    {
        BuiltinHandlerActivator _server;
        string _serverInputQueueName;
        IBus _client;

        protected override void SetUp()
        {
            _serverInputQueueName = TestConfig.GetName("server");
            
            _server = Using(new BuiltinHandlerActivator());

            Configure.With(_server)
                .Transport(t => t.UseMsmq(_serverInputQueueName))
                .Start();

            _client = Configure.With(Using(new BuiltinHandlerActivator()))
                .Transport(t => t.UseMsmqAsOneWayClient())
                .Routing(r => r.TypeBased().Map<string>(_serverInputQueueName))
                .Start();
        }

        [Test]
        public async Task OneWayClientWorks()
        {
            var gotIt = new ManualResetEvent(false);

            _server.Handle<string>(async str =>
            {
                Console.WriteLine($"Got string: {str}");
                gotIt.Set();
            });

            await _client.Send("w000000h000000!!!!1111");

            gotIt.WaitOrDie(TimeSpan.FromSeconds(3));
        }
    }
}