using System.Threading.Tasks;

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
        /// <summary>
        /// Handler method that will be called by the dispatcher for each logical message contained in the received transport message
        /// </summary>
        void Handle(TMessage message);
    }

    /// <summary>
    /// Special message handler interface that returns a <see cref="Task"/> which enables doing <code>async</code>/<code>await</code> inside
    /// message handlers
    /// </summary>
    public interface IHandleMessagesAsync<in TMessage> : IHandleMessages
    {
        /// <summary>
        /// Handler method that will be called by the dispatcher for each logical message contained in the received transport message.
        /// Must either return a <see cref="Task"/> explicitly, or implicitly by declaring the method with the <code>async</code> keyword -
        /// e.g. <code>async Task Handle(MyMessage message) {
        ///     await SomethingAsync();
        /// }</code>
        /// </summary>
        Task Handle(TMessage message);
    }
}