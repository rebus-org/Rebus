namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of sending and receiving messages at the same time.
    /// </summary>
    public interface IDuplexTransport : ISendMessages, IReceiveMessages
    {
    }
}