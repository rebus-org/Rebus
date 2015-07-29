using Rebus.Bus;

namespace Rebus.Tests
{
    class IncomingMessageMutatorPipelineForTesting : IMutateIncomingMessages
    {
        public object MutateIncoming(object message)
        {
            return message;
        }
    }
}