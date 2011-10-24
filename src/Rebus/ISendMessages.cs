namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of sending a <see cref="TransportMessage"/> somewhere.
    /// </summary>
    public interface ISendMessages
    {
        void Send(string recipient, TransportMessage message);
    }
}