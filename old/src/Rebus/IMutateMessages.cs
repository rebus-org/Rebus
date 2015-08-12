namespace Rebus
{
    /// <summary>
    /// Implement this in order to get a hook that gets to mutate incoming and outgoing messages.
    /// </summary>
    public interface IMutateMessages
    {
        /// <summary>
        /// Will be called once for each incoming logical message before it gets dispatched to handlers.
        /// </summary>
        object MutateIncoming(object message);
        
        /// <summary>
        /// Will be called once for each outgoing logical message before it gets added to the outgoing
        /// transport message.
        /// </summary>
        object MutateOutgoing(object message);
    }
}