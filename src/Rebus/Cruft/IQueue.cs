namespace Rebus.Cruft
{
    public interface IQueue
    {
        ISendMessages GetSender(string endpoint);
        IReceiveMessages GetReceiver();
    }
}