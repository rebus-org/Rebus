using Rebus.Messages;

namespace Rebus
{
    public interface IReceiveMessages
    {
        TransportMessage ReceiveMessage();
        string InputQueue { get; }
    }
}