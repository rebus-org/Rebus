namespace Rebus.Bus
{
    interface IMutateIncomingMessages
    {
        object MutateIncoming(object message);
    }
}