namespace Rebus
{
    public interface IMessageQueue
    {
        object ReceiveMessage();
        void Send(string recipient, object message);
    }
}