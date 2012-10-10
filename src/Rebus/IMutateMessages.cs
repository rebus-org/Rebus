namespace Rebus
{
    public interface IMutateMessages
    {
        object MutateIncoming(object message);
        object MutateOutgoing(object message);
    }
}