namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of sending and receiving messages at the same time, using the
    /// same channel type thus allowing it to send messages to itself.
    /// </summary>
    public interface IDuplexTransport : ISendMessages, IReceiveMessages
    {
    }
}