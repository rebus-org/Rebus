using GoCommando;
using GoCommando.Api;
using GoCommando.Attributes;
using Rebus.Forklift.Common;
using Rebus.Logging;
using Rebus.Transport.Msmq;

namespace Rebus.Forklift.Msmq
{
    [Banner(@"Rebus Forklift - simple message mover - MSMQ edition")]
    class Program : ICommando
    {
        [PositionalArgument]
        [Description("Name of queue to receive messages from")]
        [Example("some_queue")]
        [Example("remote_queue@another_machine")]
        public string InputQueue { get; set; }

        [NamedArgument("output", "o")]
        [Description("Default queue to forward messages to")]
        [Example("another_queue")]
        [Example("remote_queue@another_machine")]
        public string DefaultOutputQueue { get; set; }

        static void Main(string[] args)
        {
            Go.Run<Program>(args);
        }

        public void Run()
        {
            RebusLoggerFactory.Current = new NullLoggerFactory();

            var transport = new MsmqTransport(InputQueue);

            var returnToSourceQueue = new ReturnToSourceQueue(transport)
            {
                InputQueue = InputQueue,
                DefaultOutputQueue = DefaultOutputQueue
            };

            returnToSourceQueue.Run();
        }
    }
}
