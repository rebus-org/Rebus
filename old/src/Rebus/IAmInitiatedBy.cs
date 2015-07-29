namespace Rebus
{
    /// <summary>
    /// Special handler implementation that is allowed to initiate a new saga instance when
    /// the incoming message cannot be correlated with an existing saga.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being handled. Can be any assignable type as well.</typeparam>
    public interface IAmInitiatedBy<TMessage> : IHandleMessages<TMessage>
    {
    }

    /// <summary>
    /// Special handler implementation that is allowed to initiate a new saga instance when
    /// the incoming message cannot be correlated with an existing saga - this is the async variant
    /// and is to <see cref="IAmInitiatedBy{TMessage}"/> as <see cref="IHandleMessagesAsync{TMessage}"/> is
    /// to <see cref="IHandleMessages{TMessage}"/>
    /// </summary>
    /// <typeparam name="TMessage">The type of the message being handled. Can be any assignable type as well.</typeparam>
    public interface IAmInitiatedByAsync<TMessage> : IHandleMessagesAsync<TMessage>
    {
    }
}