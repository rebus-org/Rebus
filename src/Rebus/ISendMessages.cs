namespace Rebus
{
    public interface ISendMessages
    {
        void Send(string recipient, TransportMessage message);
    }
}