using NUnit.Framework;
using Rebus.Activation;
using Rebus.Config;
using Rebus.Tests.Assumptions.Worker.Messages;
using Rebus.Transport.Msmq;

namespace Rebus.Tests.Assumptions
{
    [TestFixture]
    public class Screenshot : FixtureBase
    {
        readonly string _inputQueueName = TestConfig.QueueName("worker");

        [Test, Ignore("to be run manually")]
        public void NizzleName()
        {
            var bus = Configure.With(new BuiltinHandlerActivator())
                .Transport(t => t.UseMsmq(_inputQueueName))
                .Start();

            Using(bus);

            bus.Advanced.Workers.SetNumberOfWorkers(0);

            bus.SendLocal(new Work
            {
                WorkId = 23,
                SomeValues = new[] {"hej", "med", "dig", "min", "ven", ":)"}
            }).Wait();
        }
    }

    namespace Worker.Messages
    {
        public class Work
        {
            public int WorkId { get; set; }
            public string[] SomeValues { get; set; }
        }
    }
}