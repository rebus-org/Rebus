namespace Rebus
{
    /// <summary>
    /// Top level handler interface that allows for interacting with handlers in
    /// a message type-agnostic manner.
    /// </summary>
    public interface IHandleMessages
    {
    }

    /// <summary>
    /// This is the main message handler interface of Rebus. Implement these to be
    /// called when messages are dispatched.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being handled. Can be any assignable type as well.</typeparam>
    public interface IHandleMessages<in TMessage> : IHandleMessages
    {
        void Handle(TMessage message);
    }
}