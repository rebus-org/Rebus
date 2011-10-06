namespace Rebus
{
    /// <summary>
    /// This is the main API of Rebus. Most application code should not depend on
    /// any other operation of <see cref="RebusBus"/>.
    /// </summary>
    public interface IBus
    {
        void Send(object message);
        void Reply(object message);
        void Subscribe<TMessage>();
        void Publish(object message);
    }
}