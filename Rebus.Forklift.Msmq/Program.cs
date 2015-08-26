using GoCommando;
using GoCommando.Api;
using GoCommando.Attributes;
using Rebus.Forklift.Common;
using Rebus.Transport.Msmq;

namespace Rebus.Forklift.Msmq
{
    [Banner(@"Rebus Forklift - simple message mover - MSMQ edition")]
    class Program : ForkliftBase, ICommando
    {
        static void Main(string[] args)
        {
            Go.Run<Program>(args);
        }

        public void Run()
        {
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
