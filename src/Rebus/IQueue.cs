namespace Rebus
{
    public interface IQueue
    {
        ISendMessages GetSender(string endpoint);
        IReceiveMessages GetReceiver();
    }
}