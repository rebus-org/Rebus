namespace Rebus
{
    public interface ISendMessages
    {
        void Send(string recipient, object message);
    }
}