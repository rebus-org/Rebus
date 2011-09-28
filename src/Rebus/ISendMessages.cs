namespace Rebus
{
    public interface ISendMessages
    {
        void Send(string recipient, TransportMessage message);
    }

    public class TransportMessage
    {
        public string ReturnAddress { get; set; }
        public object[] Messages { get; set; }
    }
}