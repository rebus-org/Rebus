namespace Rebus
{
    /// <summary>
    /// Interface of something that is capable of sending a <see cref="TransportMessageToSend"/> somewhere.
    /// </summary>
    public interface ISendMessages
    {
        void Send(string recipient, TransportMessageToSend message);
    }
}