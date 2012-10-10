using System.Linq;

namespace Rebus.Bus
{
    class IncomingMessageMutatorPipeline : IMutateIncomingMessages
    {
        readonly IRebusEvents rebusEvents;

        public IncomingMessageMutatorPipeline(IRebusEvents rebusEvents)
        {
            this.rebusEvents = rebusEvents;
        }

        public object MutateIncoming(object message)
        {
            return rebusEvents.MessageMutators.Reverse()
                .Aggregate(message, (current, mutator) => mutator.MutateOutgoing(current));
        }
    }
}