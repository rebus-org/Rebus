using System.Linq;

namespace Rebus.Bus
{
    /// <summary>
    /// Implementation that mutates incoming messages by running the list of message
    /// mutators from the current <see cref="IRebusEvents"/> in reverse
    /// </summary>
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
                .Aggregate(message, (current, mutator) => mutator.MutateIncoming(current));
        }
    }
}