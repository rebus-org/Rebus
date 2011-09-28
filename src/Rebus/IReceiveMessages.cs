namespace Rebus
{
    public interface IReceiveMessages
    {
        TransportMessage ReceiveMessage();
        string InputQueue { get; }
    }
}