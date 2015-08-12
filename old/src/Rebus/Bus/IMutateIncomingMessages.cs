namespace Rebus.Bus
{
    /// <summary>
    /// Service that is responsible for running message mutators on incoming messages
    /// </summary>
    interface IMutateIncomingMessages
    {
        object MutateIncoming(object message);
    }
}