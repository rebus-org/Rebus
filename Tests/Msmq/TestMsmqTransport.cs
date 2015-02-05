using System;
using System.Linq;
using NUnit.Framework;
using Rebus2.Msmq;

namespace Tests.Msmq
{
    [TestFixture]
    public class TestMsmqTransport : FixtureBase
    {
        [Test]
        public void HowFast()
        {
            const string inputQueueName = "test.performance";

            MsmqUtil.EnsureMessageQueueExists(MsmqUtil.GetPath(inputQueueName));

            var transport = new MsmqTransport(inputQueueName);

            Enumerable.Range(0, 1)
                .Select(id => new SomeMessage {Id = id})
                .ToList()
                .ForEach(msg =>
                {
                    Console.WriteLine("Sending {0}", msg);
                    transport.Send(inputQueueName, TransportMessageHelpers.FromString("hej"));
                });
        }

        class SomeMessage
        {
            public int Id { get; set; }

            public override string ToString()
            {
                return string.Format("msg {0}", Id);
            }
        }
    }
}