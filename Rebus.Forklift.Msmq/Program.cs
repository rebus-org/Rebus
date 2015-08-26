using GoCommando;
using GoCommando.Attributes;
using Rebus.Forklift.Common;
using Rebus.Transport.Msmq;

namespace Rebus.Forklift.Msmq
{
    [Banner(@"Rebus Forklift - simple message mover - MSMQ edition")]
    class Program : ForkliftBase
    {
        static void Main(string[] args)
        {
            Go.Run<Program>(args);
        }

        protected override void DoRun()
        {
            using (var transport = new MsmqTransport(InputQueue))
            {
                var returnToSourceQueue = new ReturnToSourceQueue(transport)
                {
                    InputQueue = InputQueue,
                    DefaultOutputQueue = DefaultOutputQueue
                };

                returnToSourceQueue.Run();
            }
        }
    }
}
